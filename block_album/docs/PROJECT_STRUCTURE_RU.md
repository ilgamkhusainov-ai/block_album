# Структура проекта (Unity, MVP)

## Папки
- `UnityProject/Assets/_Project/Scenes` - сцены (`Bootstrap`, `Gameplay`, `Meta`).
- `UnityProject/Assets/_Project/Scripts` - код.
- `UnityProject/Assets/_Project/Prefabs` - префабы поля, ячеек, фигур, блокеров, UI.
- `UnityProject/Assets/_Project/UI` - экраны и HUD.
- `UnityProject/Assets/_Project/Art` - спрайты, атласы.
- `UnityProject/Assets/_Project/VFX` - визуальные эффекты.
- `UnityProject/Assets/_Project/Audio` - SFX и музыка.
- `UnityProject/Assets/_Project/Data` - json/таблицы уровней.
- `UnityProject/Assets/_Project/ScriptableObjects` - конфиги геймдизайна.
- `UnityProject/Assets/_Project/Tools` - служебные editor-утилиты.

## Рекомендуемая структура кода
- `Scripts/Core` - bootstrap, service locator, game state.
- `Scripts/Grid` - поле, ячейки, проверки постановки.
- `Scripts/Pieces` - фигуры, спавн, drag-and-drop, preview.
- `Scripts/Clear` - очистки линий/зон, резолв хода.
- `Scripts/Score` - счет, комбо, формулы.
- `Scripts/Obstacles` - блокеры и их поведение.
- `Scripts/Boosters` - бомбы и swap.
- `Scripts/Goals` - цели уровня (по очкам), статус выполнения.
- `Scripts/UI` - HUD, экраны конца матча, подсказки, визуализация разбивки очков по ходу.
- `Scripts/UI/TurnScoreFeedHud.cs` - лента последних начислений с расшифровкой источников очков.
- `Scripts/UI/LevelShapePoolIntroHud.cs` - 5-секундный pre-level pop-up с пулом фигур и подсветкой новой/усиленной фигуры.
- `Scripts/UI/AutoHintController.cs` - автоподсказка после паузы (ghost лучшего валидного хода), отключается при overlay.
- `Scripts/Goals/LevelProgressionController.cs` - progression по уровням (конфиг/генерация целей, переход на следующий уровень, tier-сложность по вариативности фигур).
- `Scripts/Meta` - daily login, задания, награды.
- `Scripts/Ads` - rewarded ads абстракция.
- `Scripts/Config` - ScriptableObject-конфиги.

## Базовые ScriptableObject-конфиги
- `BoardConfig` - размер поля, стартовые ограничения.
- `PieceCatalogConfig` - наборы фигур по этапам.
- `ScoringConfig` - очки, комбо-множители, заряд силы.
- `ObstacleConfig` - частота движения/спавна блокеров.
- `BoosterConfig` - параметры 3 бомб и swap.
- `LevelGoalsConfig` - цели и параметры уровней.
- `HintConfig` - задержка автоподсказки.

## Контроль scope
- Любая новая механика попадает в backlog и не идет в код до завершения MVP-критериев.
- Оптимизация и полировка только после стабилизации core loop.
