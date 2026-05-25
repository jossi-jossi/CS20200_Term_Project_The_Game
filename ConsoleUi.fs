namespace TheGame

open System

module ConsoleUi =
    /// prints a prompt, reads one line from the terminal, and removes extra spaces.
    /// Returns the trimmed input string, or "" if input is unavailable.
    let private readTrimmed (prompt: string) =
        Console.Write(prompt)
        let line = Console.ReadLine()
        if isNull line then "" else line.Trim()

    let private waitForEnter () =
        Console.Write("Press Enter to continue...")
        Console.ReadLine() |> ignore

    /// clear screen for game state updates
    let private clearScreen () =
        try
            Console.Clear()
        with _ ->
            printfn ""

    /// Redraws the full game screen instead of printing a long history.
    /// This helps hide previous stack tops in a terminal-based game.
    let private showScreen game messages =
        clearScreen ()
        printfn "The Game: Play... as long as you can!"
        printfn ""

        /// Each stack is drawn like a small card. Only the current top is shown.
        let cardLines label direction top =
            [ "+----------+"
              sprintf "| %5s    |" label
              sprintf "|%-9s|" direction
              sprintf "| %5i    |" top
              "+----------+" ]

        let cards =
            [ cardLines "A" "(1 -> 100)" game.Table.A
              cardLines "B" "(1 -> 100)" game.Table.B
              cardLines "C" "(100 -> 1)" game.Table.C
              cardLines "D" "(100 -> 1)" game.Table.D
              [ "+----------+"
                "|  E(deck) |"
                "|cards left|"
                sprintf "|%6i    |" game.Deck.Length
                "+----------+" ] ]

        printfn ""
        for row in 0..4 do
            cards
            |> List.map (List.item row)
            |> String.concat "  "
            |> printfn "%s"

        printfn ""
        printfn "Your hand: %s" (game.User.Hand |> List.map string |> String.concat " ")

        match messages with
        | [] -> ()
        | _ ->
            printfn ""
            printfn "[Latest messages]"
            messages |> List.iter (printfn "%s")

        printfn ""

    /// user chooses the game mode
    /// Returns SinglePlayer or WithComputer.
    let private chooseMode () =
        let rec loop () =
            match readTrimmed "Choose mode: single(s) or computer(c) > " with
            | value when value.Equals("single", StringComparison.OrdinalIgnoreCase) -> SinglePlayer
            | value when value.Equals("s", StringComparison.OrdinalIgnoreCase) -> SinglePlayer
            | value when value.Equals("computer", StringComparison.OrdinalIgnoreCase) -> WithComputer
            | value when value.Equals("c", StringComparison.OrdinalIgnoreCase) -> WithComputer
            | _ ->
                printfn "Please type 'single(s)' or 'computer(c)'."
                loop ()

        loop ()

    /// user chooses who plays first
    /// Returns the selected first PlayerKind.
    let private chooseFirstPlayer mode =
        match mode with
        | SinglePlayer -> User
        | WithComputer ->
            let rec loop () =
                match readTrimmed "Who starts first: user(u) or computer(c) > " with
                | value when value.Equals("user", StringComparison.OrdinalIgnoreCase) -> User
                | value when value.Equals("u", StringComparison.OrdinalIgnoreCase) -> User
                | value when value.Equals("computer", StringComparison.OrdinalIgnoreCase) -> Computer
                | value when value.Equals("c", StringComparison.OrdinalIgnoreCase) -> Computer
                | _ ->
                    printfn "Please type 'user(u)' or 'computer(c)'."
                    loop ()

            loop ()

    /// Returns true if the user wants another game, otherwise false.
    let private chooseReplay () =
        let rec loop () =
            match readTrimmed "Play another game? yes(y) or no(n) > " with
            | value when value.Equals("yes", StringComparison.OrdinalIgnoreCase) -> true
            | value when value.Equals("y", StringComparison.OrdinalIgnoreCase) -> true
            | value when value.Equals("no", StringComparison.OrdinalIgnoreCase) -> false
            | value when value.Equals("n", StringComparison.OrdinalIgnoreCase) -> false
            | _ ->
                printfn "Please type 'yes(y)' or 'no(n)'."
                loop ()

        loop ()

    /// player types in a move
    /// Returns Ok(card, stack) for valid syntax, otherwise Error message.
    let private parseMove (input: string) =
        let parts = input.Split([| ' '; '\t'; ',' |], StringSplitOptions.RemoveEmptyEntries)

        if parts.Length <> 2 then
            Error "Type a card and a stack, for example: 42 A"
        else
            match Int32.TryParse(parts[0]), StackId.tryParse parts[1] with
            | (true, card), Some stack -> Ok(card, stack)
            | _ -> Error "Type a card number and one of A, B, C, or D."

    /// asks the user if it wants to comment to computer.
    /// The user may enter several stack labels, separated by spaces or commas.
    /// Returns the stacks to ask the computer to avoid.
    let private askAvoidStacks () =
        let text = readTrimmed "Comment to computer? Type stack letters like A C to ask it to avoid them, or press Enter > "

        if String.IsNullOrWhiteSpace text then
            []
        else
            /// Accepts input such as "A C", "A,C", or "a b d".
            let parts = text.Split([| ' '; '\t'; ',' |], StringSplitOptions.RemoveEmptyEntries)
            let parsed = parts |> Array.choose StackId.tryParse |> Array.toList |> List.distinct

            if parsed.IsEmpty then
                printfn "Comment ignored. Use only A, B, C, or D."
                []
            else
                /// Echo the cooperative comment without revealing exact cards.
                let stackText = parsed |> List.map StackId.name |> String.concat ", "
                printfn "You: Could you not use stack(s) %s? I have really good cards." stackText
                parsed

    /// Returns true when the game can continue, otherwise false after printing the result.
    let private printStatusAndCanContinue game =
        match Rules.statusAtTurnStart game with
        | Won ->
            printfn "All 98 cards have been played. You win!"
            false
        | Lost(_, reason) ->
            printfn "%s The players lose." reason
            false
        | InProgress -> true

    /// Returns the game state after the user's turn finishes or the game is lost.
    let rec private userTurn game =
        if not (printStatusAndCanContinue game) then
            game
        else
            let required = Rules.minCardsRequiredAtTurnStart game
            let startMessages =
                [ Computer.commentForUserTurn game
                  Some(sprintf "Your turn. You must play at least %i card(s)." required) ]
                |> List.choose id

            showScreen game startMessages

            let rec loop currentGame played =
                let remainingRequired = required - played

                /// If the user cannot still satisfy the minimum, the game ends.
                if remainingRequired > 0 && not (Rules.canPlayAtLeast remainingRequired currentGame.Table currentGame.User.Hand) then
                    showScreen currentGame [ sprintf "You cannot play the required %i more card(s). The players lose." remainingRequired ]
                    currentGame
                /// If the user has met the minimum and no moves remain, end turn.
                elif played >= required && not (Rules.hasAnyLegalMove currentGame.Table currentGame.User.Hand) then
                    showScreen currentGame [ "No more legal cards are available, so your turn ends." ]
                    Rules.finishTurn played currentGame
                else
                    let input = readTrimmed "Play '<card> <stack>', or 'end' > "

                    if input.Equals("end", StringComparison.OrdinalIgnoreCase) then
                        if played >= required then
                            Rules.finishTurn played currentGame
                        else
                            printfn "You still need to play %i more card(s)." (required - played)
                            loop currentGame played
                    else
                        match parseMove input with
                        | Error message ->
                            printfn "%s" message
                            loop currentGame played
                        | Ok(card, stack) ->
                            match Rules.tryPlayCard card stack currentGame with
                            | Error message ->
                                printfn "%s" message
                                loop currentGame played
                            | Ok(nextGame, _, quality) ->
                                let reaction =
                                    Computer.commentForUserTurn nextGame
                                    |> Option.orElseWith (fun () ->
                                        match currentGame.Mode with
                                        | WithComputer -> Some(Computer.feedback quality)
                                        | SinglePlayer -> None)

                                let messages =
                                    [ Some(sprintf "You placed a card on %s." (StackId.name stack))
                                      reaction ]
                                    |> List.choose id

                                showScreen nextGame messages
                                loop nextGame (played + 1)

            loop game 0

    /// for computer's turn
    /// Returns the game state after the computer's turn finishes or the game is lost.
    and private computerTurn game =
        /// no more cards can be placed -> returns the input game state 
        if not (printStatusAndCanContinue game) then
            game
        else
            /// minimum cards required to play at the turn
            let required = Rules.minCardsRequiredAtTurnStart game
            showScreen game [ sprintf "Computer turn. It must play at least %i card(s)." required ]

            let rec loop currentGame played lastMoveMessage =
                /// (1) has played minimum cards required
                /// (2) it doesn't have sufficiently good cards left
                if played >= required && Computer.chooseExtraMove [] currentGame |> Option.isNone then
                    match lastMoveMessage with
                    | Some message -> showScreen currentGame [ message ]
                    | None -> ()
                    Rules.finishTurn played currentGame
                else
                    /// The avoid list may contain several stacks; the computer strategy
                    /// penalizes all of them when choosing the next move.
                    let avoidStacks = askAvoidStacks ()
                    let requiredRemaining = max 0 (required - played)
                    let selectedMove =
                        /// necessary moves
                        if requiredRemaining > 0 then
                            match Computer.chooseMoves requiredRemaining avoidStacks currentGame with
                            | Error message -> Error message
                            | Ok(_, []) -> Error "Computer cannot make the required move."
                            | Ok(_, nextMove :: _) -> Ok nextMove
                        /// extra moves
                        else
                            match Computer.chooseExtraMove avoidStacks currentGame with
                            | Some nextMove -> Ok nextMove
                            | None -> Error "Computer chooses to stop; no efficient extra move is available."

                    match selectedMove with
                    | Error message ->
                        if requiredRemaining > 0 then
                            showScreen currentGame [ sprintf "%s The players lose." message ]
                            currentGame
                        else
                            let messages =
                                [ lastMoveMessage
                                  Some message ]
                                |> List.choose id

                            showScreen currentGame messages
                            Rules.finishTurn played currentGame
                    | Ok nextMove ->
                        match Rules.tryPlayCard nextMove.Card nextMove.Stack currentGame with
                        | Error message ->
                            showScreen currentGame [ sprintf "%s The players lose." message ]
                            currentGame
                        | Ok(afterMove, move, _) ->
                            let moveMessage = sprintf "Computer has placed a card on %s." (StackId.name move.Stack)
                            showScreen afterMove [ moveMessage ]
                            waitForEnter ()
                            loop afterMove (played + 1) (Some moveMessage)

            loop game 0 None

    let run () =
        let rng = Random()

        /// Plays one game until win/loss. The replay loop below can start more.
        let rec gameLoop game =
            if printStatusAndCanContinue game then
                let nextGame =
                    match game.Current with
                    | User -> userTurn game
                    | Computer -> computerTurn game

                if obj.ReferenceEquals(nextGame, game) then
                    ()
                else
                    gameLoop nextGame

        /// Outer loop: after one game ends, ask whether to start a new game.
        let rec replayLoop () =
            clearScreen ()
            printfn "The Game: Play... as long as you can!"
            let mode = chooseMode ()
            let firstPlayer = chooseFirstPlayer mode
            let initialGame = Rules.newGame mode firstPlayer rng

            gameLoop initialGame

            if chooseReplay () then
                replayLoop ()
            else
                printfn "Thanks for playing!"

        replayLoop ()
