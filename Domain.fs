namespace TheGame

// The four visible stacks in the middle of the table.
type StackId =
    | A
    | B
    | C
    | D

module StackId =
    // Useful when checking every stack for legal moves.
    // Returns the list [A; B; C; D].
    let all = [ A; B; C; D ]

    // Converts a stack value into the label printed in the UI.
    // Returns "A", "B", "C", or "D".
    let name stack =
        match stack with
        | A -> "A"
        | B -> "B"
        | C -> "C"
        | D -> "D"

    // Converts user input like "a" or "A" into a StackId.
    // Returns Some stack for valid input, otherwise None.
    let tryParse (text: string) =
        match text.Trim().ToUpperInvariant() with
        | "A" -> Some A
        | "B" -> Some B
        | "C" -> Some C
        | "D" -> Some D
        | _ -> None

type Direction =
    | Increasing
    | Decreasing

// Stores only the top card of each stack.
// This matches the requirement that previous stack cards are hidden.
type Table =
    { A: int
      B: int
      C: int
      D: int }

module Table =
    // A and B start at 1. C and D start at 100.
    // Returns the initial table state.
    let initial = { A = 1; B = 1; C = 100; D = 100 }

    // Gets the current top card for a stack.
    // Returns the integer top card.
    let top stack table =
        match stack with
        | A -> table.A
        | B -> table.B
        | C -> table.C
        | D -> table.D

    // A/B go upward. C/D go downward.
    // Returns Increasing or Decreasing.
    let direction stack =
        match stack with
        | A
        | B -> Increasing
        | C
        | D -> Decreasing

    // Returns a new table after placing a card on one stack.
    // Returns the updated Table.
    let place stack card table =
        match stack with
        | A -> { table with A = card }
        | B -> { table with B = card }
        | C -> { table with C = card }
        | D -> { table with D = card }

// The project supports single-player and user-with-computer modes.
type Mode =
    | SinglePlayer
    | WithComputer

// Identifies whose turn it is and which hand to update.
type PlayerKind =
    | User
    | Computer

module PlayerKind =
    // Returns "User" or "Computer".
    let name player =
        match player with
        | User -> "User"
        | Computer -> "Computer"

type Player =
    { Kind: PlayerKind
      Hand: int list }

// Complete game state. Most rule functions receive and return this record.
type Game =
    { Mode: Mode
      Table: Table
      Deck: int list
      User: Player
      Computer: Player option
      Current: PlayerKind }

type Move =
    { Player: PlayerKind
      Card: int
      Stack: StackId }

// Used only for computer feedback messages.
type MoveQuality =
    | ReverseTen
    | Efficient
    | Ordinary
    | Risky

// Result of checking whether the game can continue at the start of a turn.
type GameStatus =
    | InProgress
    | Won
    | Lost of PlayerKind * string
