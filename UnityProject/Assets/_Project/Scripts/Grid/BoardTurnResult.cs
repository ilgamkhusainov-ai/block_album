namespace BlockAlbum.Grid
{
    public readonly struct BoardTurnResult
    {
        public static BoardTurnResult Empty => new BoardTurnResult(
            placementSucceeded: false,
            placedCells: 0,
            clearedCells: 0,
            clearedBlockers: 0,
            linesCleared: 0,
            zonesCleared: 0,
            comboLevel: 0,
            comboFromMulti: 0,
            comboFromStreak: 0,
            clearStreak: 0,
            scoreGained: 0,
            scoreFromCells: 0,
            scoreFromBlockers: 0,
            scoreFromLines: 0,
            scoreFromZones: 0,
            comboBonusScore: 0,
            totalScore: 0,
            powerGained: 0,
            powerCharge: 0,
            powerMax: 0);

        public bool PlacementSucceeded { get; }
        public int PlacedCells { get; }
        public int ClearedCells { get; }
        public int ClearedBlockers { get; }
        public int LinesCleared { get; }
        public int ZonesCleared { get; }
        public int ComboLevel { get; }
        public int ComboFromMulti { get; }
        public int ComboFromStreak { get; }
        public int ClearStreak { get; }
        public int ScoreGained { get; }
        public int ScoreFromCells { get; }
        public int ScoreFromBlockers { get; }
        public int ScoreFromLines { get; }
        public int ScoreFromZones { get; }
        public int ComboBonusScore { get; }
        public int TotalScore { get; }
        public int PowerGained { get; }
        public int PowerCharge { get; }
        public int PowerMax { get; }

        public BoardTurnResult(
            bool placementSucceeded,
            int placedCells,
            int clearedCells,
            int clearedBlockers,
            int linesCleared,
            int zonesCleared,
            int comboLevel,
            int comboFromMulti,
            int comboFromStreak,
            int clearStreak,
            int scoreGained,
            int scoreFromCells,
            int scoreFromBlockers,
            int scoreFromLines,
            int scoreFromZones,
            int comboBonusScore,
            int totalScore,
            int powerGained,
            int powerCharge,
            int powerMax)
        {
            PlacementSucceeded = placementSucceeded;
            PlacedCells = placedCells;
            ClearedCells = clearedCells;
            ClearedBlockers = clearedBlockers;
            LinesCleared = linesCleared;
            ZonesCleared = zonesCleared;
            ComboLevel = comboLevel;
            ComboFromMulti = comboFromMulti;
            ComboFromStreak = comboFromStreak;
            ClearStreak = clearStreak;
            ScoreGained = scoreGained;
            ScoreFromCells = scoreFromCells;
            ScoreFromBlockers = scoreFromBlockers;
            ScoreFromLines = scoreFromLines;
            ScoreFromZones = scoreFromZones;
            ComboBonusScore = comboBonusScore;
            TotalScore = totalScore;
            PowerGained = powerGained;
            PowerCharge = powerCharge;
            PowerMax = powerMax;
        }
    }
}
