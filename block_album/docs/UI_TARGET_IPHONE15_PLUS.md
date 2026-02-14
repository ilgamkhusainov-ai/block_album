# UI Target Spec (iPhone 15+)

## Baseline
- Orientation: portrait only.
- Canvas reference: 1290 x 2796.
- Canvas scaler: Match Width Or Height = 0.5.
- Safe area handling: mandatory via `SafeAreaFitter`.

## Zones (inside Safe Area)
- `TopBar`: anchors Y 0.88 -> 1.00.
- `BoardArea`: anchors X 0.05 -> 0.95, Y 0.20 -> 0.85.
- `PieceTray`: anchors X 0.03 -> 0.97, Y 0.02 -> 0.18.

## Interaction rules
- Touch targets: >= 88 px logical on baseline canvas.
- Drag preview: semi-transparent ghost shape before drop.
- Main CTA priority: quick restart / next battle action visible after game over.

## Device strategy
- Layout is built by anchors + safe area instead of model-specific hardcoding.
- This keeps UI stable from iPhone 15 family up to newer tall-screen Pro Max devices.
