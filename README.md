# The Game: Play... as long as you can!

A command-line implementation of **The Game** built with **F# / .NET 10**.

Play cooperatively in either **single-player** mode or **with-computer** mode. The goal is to place all cards from 2 to 99 onto four stacks without getting stuck.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)  
  Verify with: `dotnet --version` (should show `10.x.x`)

### Run

From the project root:

```bash
dotnet run --project TheGame/TheGame.fsproj
```

If you are already inside the `TheGame` folder:

```bash
dotnet run
```

### Build

```bash
dotnet build TheGame/TheGame.fsproj
```

### Publish Self-Contained Binary

```bash
# Windows x64
dotnet publish TheGame/TheGame.fsproj -c Release -r win-x64 --self-contained

# Linux x64
dotnet publish TheGame/TheGame.fsproj -c Release -r linux-x64 --self-contained
```

---

## How to Play

### Game Modes

At the start of the game, choose one mode:

- `single` or `s`: play alone
- `computer` or `c`: play with the computer as a second player

In `with computer` mode, you also choose who starts first:

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

The computer may say:

```text
Computer: Could you not use stack(s) A, C? I have really good cards.
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

### Computer Strategy

The computer first plays the minimum required number of cards.

After that, it may play extra cards if the next move is efficient:

- a reverse-10 move, or
- a move whose distance is small enough

If no efficient extra move exists, the computer stops its turn.

### Winning & Ending

| Result | Condition |
|--------|-----------|
| **Players win** | All 98 cards are played |
| **Players lose** | The current player cannot make the required move |

After the game ends, you are asked whether to play again.

---

## Example Session

```text
The Game: Play... as long as you can!
Choose mode: single(s) or computer(c) > computer
Who starts first: user(u) or computer(c) > user

+----------+  +----------+  +----------+  +----------+  +----------+
|     A    |  |     B    |  |     C    |  |     D    |  |  E(deck) |
|(1 -> 100)|  |(1 -> 100)|  |(100 -> 1)|  |(100 -> 1)|  |cards left|
|     1    |  |     1    |  |   100    |  |   100    |  |    84    |
+----------+  +----------+  +----------+  +----------+  +----------+

Your hand: 4 6 8 23 27 50 89

[Latest messages]
Your turn. You must play at least 2 card(s).

Play '<card> <stack>', or 'end' > 4 A
Play '<card> <stack>', or 'end' > 89 C
Play '<card> <stack>', or 'end' > end
```

---

## Project Structure

```text
project-root/
├── README.md
├── REQUIREMENTS.md
└── TheGame/
    ├── TheGame.fsproj    # .NET 10 F# project file
    ├── Domain.fs         # Core types: stacks, table, players, game state
    ├── Rules.fs          # Game rules: legality, dealing, drawing, win/loss
    ├── Computer.fs       # Computer strategy and cooperation comments
    ├── ConsoleUi.fs      # Terminal UI, user input, turn loop, replay loop
    └── Program.fs        # Entry point
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

## Rules Summary

- Cards are numbered from 2 to 99.
- `A` and `B` normally increase from 1 toward 100.
- `C` and `D` normally decrease from 100 toward 1.
- Reverse-10 moves are allowed.
- In single-player mode, the user starts with 8 cards.
- In with-computer mode, the user and computer each start with 7 cards.
- While deck `E` has cards, a player must play at least 2 cards per turn.
- Once deck `E` is empty, a player must play at least 1 card per turn.
- Stack history is hidden; only the current top card is shown.
- The players win by playing all 98 cards.
- The players lose if the current player cannot make the required move.
