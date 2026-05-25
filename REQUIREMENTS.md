# The Game: Play... as long as you can!

## Overview

This project is an online implementation of the German board game *The Game: Spiel... so lange du kannst!*.

The goal of the game is for all the players to work together and successfully put down all the cards from 2 to 99 on four stacks.

There are two modes:

- `single player`
- `with computer`, where the computer will play as the second player

## Requirements

1. The 98 cards are shuffled and each player is given 8 random cards in `single player` mode or 7 random cards in `with computer` mode. All the remaining cards will be placed facedown in a separate stack, `E`.

2. There are four stacks:

   - `A`: 1 -> 100
   - `B`: 1 -> 100
   - `C`: 100 -> 1
   - `D`: 100 -> 1

   On `A` and `B`, cards can be placed only in increasing order. On `C` and `D`, cards can be placed only in decreasing order.

   Special rule: cards where the number differs exactly by 10 can be placed in the reverse order. For example, if stack `A` shows 12 and you have the card `2` in your hand at your turn, you can put it on `A`, regardless of the increasing order rule.

3. Any player can start first, and the players will take turns. At each player's turn, the player has to put down at least two cards from its hand. At the end of the player's turn, the player draws cards from `E` equal to the number of cards played. When there are no more cards in `E` at the start of a player's turn, the player has to put down at least one card from its hand. The computer will use its own strategy to make its choice.

4. In `with computer` mode, the user and the computer can communicate for cooperation.

   For example, after the user puts down his first card on `B`, the computer might say:

   > Could you not use stack B? I have a really good card.

   The type of comments will be limited to a few. These comments will be shown in the terminal. None of the comments will allow the player to specifically say which card it has in its hand.

   The computer will take the user's comment into account when making its choice.

   The computer will also give some comments on the user's move, such as:

   > nice move!

   or:

   > come on, was that your best move?

5. `A`, `B`, `C`, and `D` will only show the card on the top. Therefore, the players cannot see which cards have already been played. `E` will show the number of cards left in the stack.

6. If all the 98 cards are played, all the players win. If the user or the computer cannot put down any card, the players lose.

## Examples

### Example 1: Single Player Mode

The user chooses `single` mode at the start of the game.

Then the four stacks, `A`, `B`, `C`, and `D`, stack `E(90)`, and eight cards will be shown on the terminal.

Let's say the first eight cards are:

```text
4, 8, 23, 27, 50, 78, 89, 90
```

The user first puts 4 on `A`, and then 8 on `A`. The user ends the turn, and two cards will be drawn from `E`. This continues until the game is over.

### Example 2: With Computer Mode

The user chooses `with computer` mode at the start of the game.

Then the four stacks, `A`, `B`, `C`, and `D`, stack `E(84)`, and seven cards will be shown on the terminal.

Let's say the first seven cards are:

```text
4, 6, 8, 23, 27, 50, 89
```

The computer will also be given seven cards, but these will not be shown to the user.

The user chooses to play first. The user first puts 4 on `A`, and the computer says:

> Could you not use stack A? I have a really good card.

The user puts 89 on `C`, ends the turn, and draws two cards from `E`.

The computer puts down 5 on `A`, and the user types:

> Could you not use stack A? I have a really good card.

The computer puts 97 on `C`, ends its turn, and draws two cards from `E`.

This continues until the game is over.
