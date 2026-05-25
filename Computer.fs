namespace TheGame

module Computer =
    // Avoid requests are cooperative suggestions, but a large penalty makes
    // the computer respect all requested stacks whenever another legal path exists.
    let private avoidStackPenalty = 30

    // After the required minimum cards are played, the computer only keeps
    // playing extra cards if the move is this close or is a reverse-10 move.
    // Returns the maximum ordinary distance allowed for extra computer moves.
    let extraMoveDistanceLimit = 3

    // gives a score to a possible move
    // Returns a numeric score; lower is better.
    let private baseScore table stack card =
        if Rules.isReverseTen table stack card then
            -100
        else
            Rules.moveDistance table stack card

    // Formats multiple stacks as "A, C, D" for cooperation messages.
    // Returns a comma-separated stack label string.
    let private stackListText stacks =
        stacks
        |> List.distinct
        |> List.map StackId.name
        |> String.concat ", "

    // Builds all legal moves from the current hand and sorts them from best
    // to worst. User "avoid these stacks" comments add penalties, not bans.
    // Returns sorted (score, card, stack) tuples.
    let private scoredMoves avoidStacks table hand =
        // Remove duplicates so "A A C" behaves the same as "A C".
        let avoided = avoidStacks |> List.distinct

        hand
        |> List.collect (fun card ->
            Rules.legalStacks table card
            |> List.map (fun stack ->
                let avoidPenalty =
                    if avoided |> List.contains stack then avoidStackPenalty else 0

                (baseScore table stack card + avoidPenalty, card, stack)))
        |> List.sortBy (fun (score, card, stack) -> score, card, StackId.name stack)

    // Chooses a sequence of required moves, not just the best first card.
    // Searching sequences matters because the first move changes stack tops.
    // Returns Ok(updatedGame, moves) or Error message.
    let chooseMoves required avoidStacks game =
        let rec search currentGame movesLeft totalScore moves =
            if movesLeft <= 0 then
                [ totalScore, currentGame, List.rev moves ]
            else
                let hand = (Rules.currentPlayer currentGame).Hand

                scoredMoves avoidStacks currentGame.Table hand
                |> List.collect (fun (score, card, stack) ->
                    match Rules.tryPlayCard card stack currentGame with
                    | Error _ -> []
                    | Ok(nextGame, move, _) -> search nextGame (movesLeft - 1) (totalScore + score) (move :: moves))

        match search game required 0 [] with
        | [] -> Error "Computer cannot make the required move."
        | options ->
            let _, nextGame, moves = options |> List.minBy (fun (score, _, moves) -> score, moves.Length)
            Ok(nextGame, moves)

    // Used after the computer has already played the required minimum.
    // Good extra moves are cheap enough that the computer should keep going.
    // Returns true if the move is worth playing as an extra move.
    let isGoodExtraMove game move =
        Rules.isReverseTen game.Table move.Stack move.Card
        || Rules.moveDistance game.Table move.Stack move.Card <= extraMoveDistanceLimit

    // Returns one optional extra move. None means the computer should stop.
    // Returns Some move when a good extra move exists, otherwise None.
    let chooseExtraMove avoidStacks game =
        match chooseMoves 1 avoidStacks game with
        | Error _ -> None
        | Ok(_, move :: _) when isGoodExtraMove game move -> Some move
        | _ -> None

    // Finds every stack where the computer has a strong hidden opportunity.
    // This supports comments that mention more than one stack.
    // Returns stacks sorted from best opportunity to weakest opportunity.
    let private goodStacksForComputer game =
        match game.Computer with
        | None -> []
        | Some computer ->
            StackId.all
            |> List.choose (fun stack ->
                // For each stack, keep only the best good card the computer has.
                let bestDistance =
                    computer.Hand
                    |> List.choose (fun card ->
                        if Rules.isLegal game.Table stack card then
                            let distance = Rules.moveDistance game.Table stack card

                            if Rules.isReverseTen game.Table stack card || distance <= 4 then
                                Some distance
                            else
                                None
                        else
                            None)
                    |> List.sort
                    |> List.tryHead

                bestDistance |> Option.map (fun distance -> distance, stack))
            |> List.sortBy (fun (distance, stack) -> distance, StackId.name stack)
            |> List.map snd

    // At the start of the user's turn, the computer can warn about whichever
    // stacks currently give it strong hidden opportunities.
    // Returns Some comment if a strong hidden opportunity exists, otherwise None.
    let commentForUserTurn game =
        match goodStacksForComputer game with
        | [] -> None
        | stacks ->
            Some(sprintf "Computer: Could you not use stack(s) %s? I have really good cards." (stackListText stacks))

    // Converts move quality into a short response printed by the UI.
    // Returns the feedback string.
    let feedback quality =
        match quality with
        | ReverseTen -> "Computer: Splendid!"
        | Efficient -> "Computer: Nice move!"
        | Ordinary -> "Computer: That works."
        | Risky -> "Computer: Come on, was that your best move?"
