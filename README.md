# The Game: Play... as long as you can!

A command-line implementation of **The Game** built with **F# / .NET 10**.

Play cooperatively in either **single-player** mode or **with-computer** mode. The goal is to place all cards from 2 to 99 onto four stacks without getting stuck.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)  
  Verify with: `dotnet --version` (should show `10.x.x`)

### Run

From this project folder:

```bash
dotnet run
```

### Build

```bash
dotnet build
```

### Publish Self-Contained Binary

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained
```

---

## How to Play

### Game Modes

At the start of the game, choose one mode:

- `single` or `s`: play alone
- `computer` or `c`: play with the computer as a second player

In `with computer` mode, the cards are dealt before choosing who starts. This lets you inspect your hand first. Then choose:

- `user` or `u`
- `computer` or `c`

### Stacks

There are four playable stacks and one facedown deck:

| Stack | Direction | Rule |
|-------|-----------|------|
| `A` | 1 -> 100 | Cards normally increase |
| `B` | 1 -> 100 | Cards normally increase |
| `C` | 100 -> 1 | Cards normally decrease |
| `D` | 100 -> 1 | Cards normally decrease |
| `E` | facedown deck | Shows only cards left |

The screen shows only the current top card of `A`, `B`, `C`, and `D`.

In `with computer` mode, the screen also shows how many cards are in the computer's hand, but not the card values.

### Reverse-10 Rule

A card may be placed in the reverse direction if it differs by exactly 10.

Examples:

- If `A` shows `12`, card `2` may be placed on `A`.
- If `C` shows `88`, card `98` may be placed on `C`.

### Taking a Turn

1. The current state is displayed.
2. Type a move as `<card> <stack>`.

Example:

```text
42 A
```

3. Type `end` when you want to finish your turn.
4. You must play at least two cards while deck `E` still has cards.
5. Once deck `E` is empty, you must play at least one card.

After your turn ends, you draw cards from `E` equal to the number of cards you played. If `E` is empty, no cards are drawn.

### Computer Cooperation

In `with computer` mode, the user and computer can communicate without revealing exact card numbers.

The computer may ask you to avoid stacks:

```text
Computer: Could you not use stack(s) A, C? I have really good cards.
```

The computer may also suggest one stack that looks safer to use:

```text
Computer: I suggest using stack B.
```

Before each computer move, you can ask it to avoid one or more stacks:

```text
A C
```

or:

```text
A,C
```

The computer treats this as a strong suggestion. It will try to avoid those stacks when another reasonable move exists.

You can also suggest stacks for the computer to use. Suggested stacks receive a small score bonus in the computer strategy, so the computer is more likely to use them but is not forced to.

### Computer Strategy

The computer first plays the minimum required number of cards.

After that, it may play extra cards if the next move is efficient:

- a reverse-10 move, or
- a move whose distance is small enough

If no efficient extra move exists, the computer stops its turn.

The computer's automatic stack suggestions are limited. It suggests a stack only when the stack and its direction pair still have enough room, the computer does not have a near/reverse-10 opportunity there, and the computer has several playable cards for that stack that are too large to spend immediately.

### Winning & Ending

| Result | Condition |
|--------|-----------|
| **Players win** | All 98 cards are played |
| **Players lose** | The current player cannot make the required move |

In `with computer` mode, one player may put down all of their cards before the other. The game does not end immediately. The finished player is skipped, and the remaining player continues. If the remaining player also empties their hand, the players win. If the remaining player cannot make the required move, the players lose.

After the game ends, you are asked whether to play again.

---

## Example Session

```text
The Game: Play... as long as you can!
Choose mode: single(s) or computer(c) > computer

+----------+  +----------+  +----------+  +----------+  +----------+
|     A    |  |     B    |  |     C    |  |     D    |  |  E(deck) |
|(1 -> 100)|  |(1 -> 100)|  |(100 -> 1)|  |(100 -> 1)|  |cards left|
|     1    |  |     1    |  |   100    |  |   100    |  |    84    |
+----------+  +----------+  +----------+  +----------+  +----------+

Your hand: 4 6 8 23 27 50 89
Computer hand: 7 card(s)

[Latest messages]
Check your cards, then choose who starts first.

Who starts first: user(u) or computer(c) > user

[Latest messages]
Your turn. You must play at least 2 card(s).

Play '<card> <stack>', or 'end' > 4 A
Play '<card> <stack>', or 'end' > 89 C
Play '<card> <stack>', or 'end' > end
```

---

## Project Structure

```text
TheGame/
|-- TheGame.fsproj    # .NET 10 F# project file
|-- Domain.fs         # Core types: stacks, table, players, game state
|-- Rules.fs          # Game rules: legality, dealing, drawing, win/loss
|-- Computer.fs       # Computer strategy and cooperation comments
|-- ConsoleUi.fs      # Terminal UI, user input, turn loop, replay loop
|-- Program.fs        # Entry point
|-- README.md
`-- REQUIREMENTS.md
```

### Key Types

```fsharp
type StackId = A | B | C | D

type Mode =
    | SinglePlayer
    | WithComputer

type GameStatus =
    | InProgress
    | Won
    | Lost of PlayerKind * string
```

### Module Overview

| Module | Responsibility |
|--------|----------------|
| `Domain` | Shared data types for stacks, players, moves, and game state |
| `Rules` | Pure game logic such as legal moves, drawing, turn switching, and status checks |
| `Computer` | Computer move scoring, extra-move decisions, and cooperation comments |
| `ConsoleUi` | Terminal display, input parsing, user/computer turn flow, and replay loop |
| `Program` | Starts the console game |

---

## Changes to the Requirements

The goal of this project was to implement the game as similar as possible to the original boardgame. 

- In the requirements file, (computer mode) the user chose whether to play first or not before seeing the cards. This was changed so that 
the user first sees the cards, then chooses whether to play first not not (this is how the original game is played).

---

## Use of LLM

This project was developed with assistance from an LLM. The requested help included:

- Creating the initial F# console implementation of *The Game*.
- Separating the code into modules for domain types, rules, computer strategy, console UI, and program entry point.
- Implementing single-player mode and with-computer mode.
- Implementing card dealing, stack legality, reverse-10 moves, drawing, turn switching, win/loss detection, and replay.
- Improving the terminal UI so the board is redrawn clearly with card-like stack displays.
- Adding cooperative computer comments for avoiding stacks and suggesting stacks.
- Updating computer-mode endgame behavior so one player can finish their hand first while the other player continues.
- Changing the computer-mode start flow so the user sees their hand before deciding who starts.
- Showing the number of cards in the computer's hand without revealing the actual cards.
- Adding explanatory comments throughout the source files.
- Converting the requirements document to Markdown and creating this README.

### Manual Corrections and Reprompting

- The board display in the terminal needed follow-up prompts so the stacks looked clearer and the direction of each stack was visible.
- Several requests had to be refined because the first prompt did not contain every gameplay detail, such as when the user should choose who starts, how endgame should work in computer mode, and when the computer should suggest stacks.
- Several gameplay details were discovered only after testing, such as drawing from an empty deck, preserving computer move messages after screen redraws, and handling the case where one player finishes their hand before the other in computer mode.
- The comment system also required multiple refinements: first avoiding stacks, then multiple avoid stacks, then suggested stacks, then changing suggestions to only recommend the single best stack.

---

## Rules Summary

- Cards are numbered from 2 to 99.
- `A` and `B` normally increase from 1 toward 100.
- `C` and `D` normally decrease from 100 toward 1.
- Reverse-10 moves are allowed.
- In single-player mode, the user starts with 8 cards.
- In with-computer mode, the user and computer each start with 7 cards.
- In with-computer mode, the user sees their hand before deciding who starts.
- While deck `E` has cards, a player must play at least 2 cards per turn.
- Once deck `E` is empty, a player must play at least 1 card per turn.
- If one player empties their hand first in with-computer mode, the other player continues.
- Stack history is hidden; only the current top card is shown.
- The players win by playing all 98 cards.
- The players lose if the current player cannot make the required move.