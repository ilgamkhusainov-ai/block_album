using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockAlbum.Goals
{
    [DisallowMultipleComponent]
    public sealed class LevelProgressionController : MonoBehaviour
    {
        [Serializable]
        public struct LevelConfig
        {
            public int targetScore;
            public int varietyTier;
        }

        [Header("Configured Levels (optional)")]
        [SerializeField] private List<LevelConfig> configuredLevels = new List<LevelConfig>();
        [Header("Generated Levels (fallback)")]
        [SerializeField] private int generatedLevelCount = 40;
        [SerializeField] private int generatedStartTargetScore = 1000;
        [SerializeField] private int generatedStepTargetScore = 0;
        [SerializeField] private bool useFixedTargetScore = true;
        [Header("Variety by Level (fallback)")]
        [SerializeField] private int varietyIncreaseEveryLevels = 2;
        [SerializeField] private int fullPoolStartsAtLevel = 25;
        [SerializeField] private bool loopAfterLastLevel;

        private LevelGoalController _goalController;
        private int _currentLevelIndex;

        public int CurrentLevelNumber => _currentLevelIndex + 1;
        public int LevelCount => GetLevelCount();
        public int CurrentTargetScore => ResolveTargetScore(_currentLevelIndex);
        public int CurrentVarietyTier => ResolveVarietyTier(_currentLevelIndex);

        public event Action LevelChanged;

        private IEnumerator Start()
        {
            yield return null;
            BindGoal();
            ApplyCurrentLevelToGoal();
        }

        public string GetLevelLabel()
        {
            return $"Level {CurrentLevelNumber}/{LevelCount}";
        }

        public string GetVarietyLabel()
        {
            return $"Variety T{CurrentVarietyTier}";
        }

        public int GetVarietyTierForLevel(int levelNumber)
        {
            if (levelNumber <= 0)
            {
                return 0;
            }

            return ResolveVarietyTier(levelNumber - 1);
        }

        public void PrepareGoalForNewRun(bool previousRunWasWin)
        {
            if (previousRunWasWin)
            {
                AdvanceLevelIndex();
            }

            ApplyCurrentLevelToGoal();
        }

        public void RestartCurrentLevel()
        {
            ApplyCurrentLevelToGoal();
        }

        private void BindGoal()
        {
            _goalController = FindFirstObjectByType<LevelGoalController>();
        }

        private void AdvanceLevelIndex()
        {
            var count = GetLevelCount();
            if (count <= 1)
            {
                _currentLevelIndex = 0;
                return;
            }

            if (_currentLevelIndex < count - 1)
            {
                _currentLevelIndex++;
                return;
            }

            _currentLevelIndex = loopAfterLastLevel ? 0 : count - 1;
        }

        private void ApplyCurrentLevelToGoal()
        {
            if (_goalController == null)
            {
                BindGoal();
            }

            if (_goalController == null)
            {
                return;
            }

            _goalController.SetTargetScore(CurrentTargetScore, true);
            LevelChanged?.Invoke();
        }

        private int GetLevelCount()
        {
            if (configuredLevels != null && configuredLevels.Count > 0)
            {
                return configuredLevels.Count;
            }

            return Mathf.Max(1, generatedLevelCount);
        }

        private int ResolveTargetScore(int index)
        {
            if (useFixedTargetScore)
            {
                if (configuredLevels != null && configuredLevels.Count > 0)
                {
                    return Mathf.Max(1, configuredLevels[0].targetScore);
                }

                return Mathf.Max(1, generatedStartTargetScore);
            }

            if (configuredLevels != null && configuredLevels.Count > 0)
            {
                var safeIndex = Mathf.Clamp(index, 0, configuredLevels.Count - 1);
                return Mathf.Max(1, configuredLevels[safeIndex].targetScore);
            }

            var safeGeneratedIndex = Mathf.Max(0, index);
            return Mathf.Max(1, generatedStartTargetScore + safeGeneratedIndex * generatedStepTargetScore);
        }

        private int ResolveVarietyTier(int index)
        {
            const int fullPoolTier = 13;
            if (configuredLevels != null && configuredLevels.Count > 0)
            {
                var safeIndex = Mathf.Clamp(index, 0, configuredLevels.Count - 1);
                var configuredTier = configuredLevels[safeIndex].varietyTier;
                if (configuredTier > 0)
                {
                    return Mathf.Clamp(configuredTier, 1, fullPoolTier);
                }
            }

            var level = Mathf.Max(1, index + 1);
            var safeFullPoolLevel = Mathf.Max(2, fullPoolStartsAtLevel);
            if (level >= safeFullPoolLevel)
            {
                return fullPoolTier;
            }

            var step = Mathf.Max(1, varietyIncreaseEveryLevels);
            var preFullTier = 1 + (level - 1) / step;
            return Mathf.Clamp(preFullTier, 1, fullPoolTier - 1);
        }
    }
}
