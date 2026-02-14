using System;
using System.Collections;
using BlockAlbum.Grid;
using UnityEngine;

namespace BlockAlbum.Goals
{
    [DisallowMultipleComponent]
    public sealed class LevelGoalController : MonoBehaviour
    {
        [SerializeField] private int targetScore = 1200;

        private BoardView _boardView;
        private bool _isCompleted;
        private int _lastObservedTurn;
        private int _baseTargetScore;
        private bool _baseCaptured;

        public int TargetScore => targetScore;
        public int TurnsRemaining => -1;
        public bool IsCompleted => _isCompleted;
        public bool IsFailed => false;
        public int CurrentScore => _boardView != null ? _boardView.Score : 0;

        public event Action GoalChanged;

        private IEnumerator Start()
        {
            yield return null;
            BindBoard();
            CaptureBaseValuesIfNeeded();
            ResetGoalState();
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.TurnResolved -= OnTurnResolved;
            }
        }

        private void BindBoard()
        {
            _boardView = FindFirstObjectByType<BoardView>();
            if (_boardView == null)
            {
                return;
            }

            _boardView.TurnResolved -= OnTurnResolved;
            _boardView.TurnResolved += OnTurnResolved;
        }

        [ContextMenu("Goal/Reset")]
        public void ResetGoalState()
        {
            CaptureBaseValuesIfNeeded();
            targetScore = Mathf.Max(1, _baseTargetScore);
            _isCompleted = false;
            _lastObservedTurn = _boardView != null ? _boardView.PlayerTurnCount : 0;
            GoalChanged?.Invoke();
        }

        public string GetStatusLabel()
        {
            if (_isCompleted)
            {
                return $"Goal: {CurrentScore}/{targetScore}  WIN";
            }

            return $"Goal: {CurrentScore}/{targetScore}";
        }

        public void SetTargetScore(int value, bool updateBase = true)
        {
            targetScore = Mathf.Max(1, value);
            if (updateBase)
            {
                _baseTargetScore = targetScore;
                _baseCaptured = true;
            }

            GoalChanged?.Invoke();
        }

        public bool GrantExtraTurns(int extraTurns)
        {
            return false;
        }

        private void CaptureBaseValuesIfNeeded()
        {
            if (_baseCaptured)
            {
                return;
            }

            _baseTargetScore = Mathf.Max(1, targetScore);
            _baseCaptured = true;
        }

        private void OnTurnResolved(BoardTurnResult turnResult)
        {
            if (_boardView == null || !turnResult.PlacementSucceeded)
            {
                return;
            }

            var currentTurn = _boardView.PlayerTurnCount;
            if (currentTurn == _lastObservedTurn)
            {
                return;
            }

            _lastObservedTurn = currentTurn;

            if (_isCompleted)
            {
                GoalChanged?.Invoke();
                return;
            }

            if (CurrentScore >= targetScore)
            {
                _isCompleted = true;
            }

            GoalChanged?.Invoke();
        }
    }
}
