namespace TheGame

open System

module Rules =
    /// initial cards 
    /// Returns all playable cards, 2 through 99.
    let allCards = [ 2..99 ]

    /// remove a card from a players hand 
    /// Returns Some list without the first matching card, or None if missing.
    let private removeFirst value values =
        let rec loop acc remaining =
            match remaining with
            | [] -> None
            | head :: tail when head = value -> Some(List.rev acc @ tail)
            | head :: tail -> loop (head :: acc) tail

        loop [] values

    /// this keeps the cards in the player's hand sorted
    /// Returns the sorted hand.
    let sortHand hand = hand |> List.sort

    /// shuffles the cards
    /// Returns a new list containing the same cards in random order.
    let shuffle (rng: Random) cards =
        let values = cards |> List.toArray

        for index in values.Length - 1 .. -1 .. 1 do
            let other = rng.Next(index + 1)
            let temp = values[index]
            values[index] <- values[other]
            values[other] <- temp

        values |> Array.toList

    /// Splits a list into "drawn" and "remaining" parts.
    /// It is safe even when the deck has fewer cards than requested.
    /// Returns (first up-to-count items, remaining items).
    let private take count values =
        let actualCount = min count (List.length values)
        values |> List.truncate actualCount, values |> List.skip actualCount

    /// distributes the cards to the players from the shuffled cards 
    /// and set initial configurations for the game
    /// Returns a Game initialized for the selected mode.
    let dealFromDeck mode firstPlayer shuffledCards =
        match mode with
        | SinglePlayer ->
            let userHand, deck = take 8 shuffledCards

            { Mode = SinglePlayer
              Table = Table.initial
              Deck = deck
              User = { Kind = User; Hand = sortHand userHand }
              Computer = None
              Current = User }

        | WithComputer ->
            let userHand, afterUser = take 7 shuffledCards
            let computerHand, deck = take 7 afterUser

            { Mode = WithComputer
              Table = Table.initial
              Deck = deck
              User = { Kind = User; Hand = sortHand userHand }
              Computer = Some { Kind = Computer; Hand = sortHand computerHand }
              Current = firstPlayer }

    /// creates a fresh shuffled game
    /// Returns a new Game.
    let newGame mode firstPlayer rng =
        allCards |> shuffle rng |> dealFromDeck mode firstPlayer

    /// checks if the card can be placed on the given stack 
    /// Returns true if the move is legal, otherwise false.
    let isLegal table stack card =
        let top = Table.top stack table

        match Table.direction stack with
        | Increasing -> card > top || card = top - 10
        | Decreasing -> card < top || card = top + 10

    /// returns all the stacks where the card can be placed
    /// Returns a list of legal StackId values.
    let legalStacks table card =
        StackId.all |> List.filter (fun stack -> isLegal table stack card)

    /// checks if there is at least one stack where a card can be placed
    /// Returns true if any card in the hand has at least one legal stack.
    let hasAnyLegalMove table hand =
        hand |> List.exists (fun card -> legalStacks table card |> List.isEmpty |> not)

    /// switch turns
    /// Returns the game with the given player's hand/state written back.
    let private applyToCurrentPlayer player game =
        match player.Kind with
        | User -> { game with User = player }
        | Computer -> { game with Computer = Some player }

    /// game state updated
    /// Returns the Player whose turn it currently is.
    let currentPlayer game =
        match game.Current with
        | User -> game.User
        | Computer ->
            match game.Computer with
            | Some computer -> computer
            | None -> failwith "Computer player requested in single-player mode."

    /// Returns the game with Current advanced to the next player.
    let private switchCurrent game =
        match game.Mode, game.Current with
        | SinglePlayer, _ -> { game with Current = User }
        | WithComputer, User -> { game with Current = Computer }
        | WithComputer, Computer -> { game with Current = User }

    /// required cards per turn
    /// Returns 2 while deck E has cards, otherwise 1.
    let minCardsRequiredAtTurnStart game =
        if List.isEmpty game.Deck then 1 else 2

    /// Checks whether the player can play (count) cards in sequence.
    /// This must simulate each card because one move changes stack tops.
    /// Returns true if such a sequence exists.
    let rec canPlayAtLeast count table hand =
        if count <= 0 then
            true
        else
            hand
            |> List.exists (fun card ->
                legalStacks table card
                |> List.exists (fun stack ->
                    match removeFirst card hand with
                    | None -> false
                    | Some remainingHand ->
                        let nextTable = Table.place stack card table
                        canPlayAtLeast (count - 1) nextTable remainingHand))

    /// reverse play
    /// Returns true if the move uses the exact reverse-10 exception.
    let isReverseTen table stack card =
        let top = Table.top stack table

        match Table.direction stack with
        | Increasing -> card = top - 10
        | Decreasing -> card = top + 10

    /// move distance
    /// Returns how far the card moves the stack; reverse-10 returns -10.
    let moveDistance table stack card =
        let top = Table.top stack table

        if isReverseTen table stack card then
            -10
        else
            match Table.direction stack with
            | Increasing -> card - top
            | Decreasing -> top - card

    /// used for computer feedback
    /// Returns ReverseTen, Efficient, Ordinary, or Risky.
    let qualityOfMove table stack card =
        if isReverseTen table stack card then
            ReverseTen
        else
            let distance = moveDistance table stack card

            if distance <= 5 then Efficient
            elif distance <= 20 then Ordinary
            else Risky

    /// a player tries to play a card
    /// Returns Ok(updatedGame, move, quality) or Error message.
    let tryPlayCard card stack game =
        let player = currentPlayer game

        match removeFirst card player.Hand with
        | None -> Error(sprintf "%i is not in %s's hand." card (PlayerKind.name player.Kind))
        | Some _ when not (isLegal game.Table stack card) ->
            Error(sprintf "%i cannot be placed on stack %s right now." card (StackId.name stack))
        | Some remainingHand ->
            let move =
                { Player = player.Kind
                  Card = card
                  Stack = stack }

            let updatedPlayer = { player with Hand = sortHand remainingHand }
            let updatedGame =
                { game with Table = Table.place stack card game.Table }
                |> applyToCurrentPlayer updatedPlayer

            Ok(updatedGame, move, qualityOfMove game.Table stack card)

    /// Draws up to count cards from the deck.
    /// If the deck is empty, this simply returns an empty draw.
    /// Returns (drawnCards, remainingDeck).
    let draw count deck =
        take count deck

    /// end of a player's turn
    /// Returns the game after drawing cards and switching turns.
    let finishTurn cardsPlayed game =
        let player = currentPlayer game
        let drawn, remainingDeck = draw cardsPlayed game.Deck
        let updatedPlayer = { player with Hand = sortHand (player.Hand @ drawn) }

        { game with Deck = remainingDeck }
        |> applyToCurrentPlayer updatedPlayer
        |> switchCurrent

    /// Counts all cards still held by user and computer.
    /// Returns the total number of cards in all player hands.
    let totalCardsInHands game =
        let computerCount =
            game.Computer
            |> Option.map (fun computer -> computer.Hand.Length)
            |> Option.defaultValue 0

        game.User.Hand.Length + computerCount

    /// checks game status at the start of a turn
    /// Returns InProgress, Won, or Lost.
    let statusAtTurnStart game =
        if List.isEmpty game.Deck && totalCardsInHands game = 0 then
            Won
        else
            let player = currentPlayer game
            let required = minCardsRequiredAtTurnStart game

            if canPlayAtLeast required game.Table player.Hand then
                InProgress
            else
                let reason =
                    if hasAnyLegalMove game.Table player.Hand then
                        sprintf "%s cannot play the required %i card(s)." (PlayerKind.name player.Kind) required
                    else
                        sprintf "%s cannot put down any card." (PlayerKind.name player.Kind)

                Lost(player.Kind, reason)
