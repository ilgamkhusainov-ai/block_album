# Progress

## Day 1 status
- [x] Unity project skeleton prepared in `UnityProject/`.
- [x] Unity version pinned to `6000.3.8f1`.
- [x] Runtime portrait setup script added.
- [x] Safe area fitter added for notch/home indicator devices.
- [x] Editor menu tool added to generate `Bootstrap` and `Gameplay` scenes.
- [x] iPhone 15+ UI target spec documented.

## Day 2 status
- [x] Added `BoardModel` (9x9 data model with occupancy/place checks).
- [x] Added `BoardView` (runtime grid rendering for board cells).
- [x] Added `PieceShapeLibrary` with starter shapes.
- [x] Added `PieceTrayView` (3-slot tray with piece previews).
- [x] Added scene auto-bootstrap that attaches board/tray components in Gameplay scene.
- [x] Expanded `PieceShapeLibrary`: unique rotations and mirrored variants for base shapes.

## Day 3 status
- [x] Added drag-and-drop for tray pieces.
- [x] Added ghost preview on board (green valid / red invalid).
- [x] Added placement validation and commit flow on drop.
- [x] Slot is refilled with a new piece after successful placement.
- [x] Added EventSystem auto-creation fix (drag works in scenes created before this patch).
- [x] Fixed Canvas rebuild-loop error by deferred UI initialization (tray/grid build one frame later).
- [x] Added explicit wait for Canvas rebuild completion via `CanvasUpdateRegistry` before UI generation.
- [x] Drag UX updated: slot piece hides while dragging (no duplicate in tray), shows back on failed drop.
- [x] Piece tray and drag visual backgrounds removed (only piece cells are visible).

## Day 4 status
- [x] Added line clear resolver (horizontal + vertical full lines).
- [x] Added 3x3 zone clear resolver.
- [x] Added base score formula for placement and clears.
- [x] Added top bar score HUD with turn gain details.
- [x] Added clear visual separation of fixed 3x3 zones on board to make zone logic readable.
- [x] Score tuning update: no score for plain placement.
- [x] Score tuning update: when line/zone clear happens, cell score is suppressed (line/zone score only).

## Day 5 status
- [x] Added combo chains from two sources: multi-clear and clear streak.
- [x] Added combo bonus score per combo level.
- [x] Added power charge gain from combo only (no clear/no combo => no charge).
- [x] Added combo/power feedback in top HUD.
- [x] Updated streak rule: streak resets on the 3rd turn without clear.

## Day 6 status
- [x] Added blocker cells as separate board layer (visual + logical).
- [x] Blockers now block piece placement.
- [x] Implemented blocker movement every 2 player turns.
- [x] Added HUD hint for turns until next blocker move.
- [x] Blockers now move only to adjacent cells (including diagonals).
- [x] Blockers can spawn/move onto occupied figure cells.
- [x] Blockers are destroyed by clear; blocker-on-figure leaves the figure cell after blocker removal.
- [x] Added per-blocker respawn cooldown: can reappear only after 10 turns.
- [x] Added separate visual style for blocker-on-empty vs blocker-on-figure.
- [x] Clear-fill rule tuned: blocker on empty cell does NOT count as fill for line/3x3 clear.
- [x] Blocker movement now prioritizes adjacent occupied cells over empty cells.

## Day 7 status
- [x] Added blocker pressure tuning controls (`maxBlockersOnFigureCells`, `minBlockerChebyshevDistance`).
- [x] Movement target validation now enforces fairness constraints for move/spawn/respawn.
- [x] Kept movement rule: blockers move only to adjacent cells (including diagonals).

## Day 8 status
- [x] Added booster framework with runtime UI buttons (`Swap`, `Bomb H`, `Bomb V`, `Bomb 3x3`).
- [x] Bombs are wired to board actions via board tap targeting.
- [x] Added `BoardCellClicked` event and board bomb APIs in `BoardView`.
- [x] Added minimal level-goal controller (target score) and HUD wiring.
- [x] Added booster resource gating by power charge (combo-powered), with per-booster power cost.
- [x] Added live HUD refresh for non-turn power spends (e.g. `Swap`).
- [x] Booster clear no longer grants power.
- [x] Boosters only trigger when tapped cell is occupied by figure or blocker.
- [x] Booster scoring rule: +1 per cleared figure cell for booster clears.

## Day 9 status
- [x] Added end-of-run detector for fail by no valid placement from current tray pieces.
- [x] Added run-end overlay (`YOU WIN` / `RUN OVER`) with score, goal status and reason.
- [x] Added quick restart CTA (`PLAY AGAIN`) with full soft reset (board/tray/goals/boosters).
- [x] Added second-chance flow (`SECOND CHANCE`) that continues only when a valid move is guaranteed.
- [x] Added fallback second-chance board opening when tray reroll still gives no valid moves.
- [x] Added per-turn score breakdown visualization (TopBar breakdown + in-game turn feed).

## Next (Day 10)
- Add configurable level list (up to 40 levels) with target score per level.
- Add simple progression manager (complete level -> next level, fail -> retry).
