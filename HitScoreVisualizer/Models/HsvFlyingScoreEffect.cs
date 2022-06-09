using System.Collections.Generic;
using HitScoreVisualizer.HarmonyPatches;
using HitScoreVisualizer.Services;
using HitScoreVisualizer.Settings;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace HitScoreVisualizer.Models
{
	internal sealed class HsvFlyingScoreEffect : FlyingScoreEffect
	{
		private JudgmentService _judgmentService = null!;
		private Configuration? _configuration;

		private NoteCutInfo? _noteCutInfo;

		private NoteCutSpawnerPatch? _directionProvider;

		[Inject]
		internal void Construct(JudgmentService judgmentService, ConfigProvider configProvider, NoteCutSpawnerPatch directionProvider)
		{
			_judgmentService = judgmentService;
			_configuration = configProvider.GetCurrentConfig();

			_directionProvider = directionProvider;
		}

		public override void InitAndPresent(IReadonlyCutScoreBuffer cutScoreBuffer, float duration, Vector3 targetPos, Color color)
		{
			_noteCutInfo = cutScoreBuffer.noteCutInfo;

			if (_configuration != null)
			{
				if (_configuration.FixedPosition != null)
				{
					// Set current and target position to the desired fixed position
					targetPos = _configuration.FixedPosition.Value;
					transform.position = targetPos;
				}
				else if (_configuration.TargetPositionOffset != null)
				{
					targetPos += _configuration.TargetPositionOffset.Value;
				}
			}

			_color = color;
			_cutScoreBuffer = cutScoreBuffer;
			if (!cutScoreBuffer.isFinished)
			{
				cutScoreBuffer.RegisterDidChangeReceiver(this);
				cutScoreBuffer.RegisterDidFinishReceiver(this);
				_registeredToCallbacks = true;
			}

			if (_configuration == null || _noteCutInfo!.Value.noteData.gameplayType is not NoteData.GameplayType.Normal)
			{
				_text.text = cutScoreBuffer.cutScore.ToString();
				_maxCutDistanceScoreIndicator.enabled = cutScoreBuffer.centerDistanceCutScore == cutScoreBuffer.noteScoreDefinition.maxCenterDistanceCutScore;
				_colorAMultiplier = (double) cutScoreBuffer.cutScore > (double) cutScoreBuffer.maxPossibleCutScore * 0.9f ? 1f : 0.3f;
			}
			else
			{
				_maxCutDistanceScoreIndicator.enabled = false;

				// Apply judgments a total of twice - once when the effect is created, once when it finishes.
				Judge((CutScoreBuffer) cutScoreBuffer, 30);
			}

			InitAndPresent(duration, targetPos, cutScoreBuffer.noteCutInfo.worldRotation, false);
		}

		protected override void ManualUpdate(float t)
		{
			var color = _color.ColorWithAlpha(_fadeAnimationCurve.Evaluate(t));
			_text.color = color;
			_maxCutDistanceScoreIndicator.color = color;
		}

		public override void HandleCutScoreBufferDidChange(CutScoreBuffer cutScoreBuffer)
		{
			if (_configuration == null || _noteCutInfo!.Value.noteData.gameplayType is not NoteData.GameplayType.Normal)
			{
				base.HandleCutScoreBufferDidChange(cutScoreBuffer);
				return;
			}

			if (_configuration.DoIntermediateUpdates)
			{
				Judge(cutScoreBuffer);
			}
		}

		public override void HandleCutScoreBufferDidFinish(CutScoreBuffer cutScoreBuffer)
		{
			if (_configuration != null && _noteCutInfo!.Value.noteData.gameplayType is NoteData.GameplayType.Normal)
			{
				Judge(cutScoreBuffer);
			}

			base.HandleCutScoreBufferDidFinish(cutScoreBuffer);
		}

		private void Judge(CutScoreBuffer cutScoreBuffer, int? assumedAfterCutScore = null)
		{
			var before = cutScoreBuffer.beforeCutScore;
			var after = assumedAfterCutScore ?? cutScoreBuffer.afterCutScore;
			var accuracy = cutScoreBuffer.centerDistanceCutScore;
			var total = before + after + accuracy;
			var timeDependence = Mathf.Abs(_noteCutInfo!.Value.cutNormal.z);
            OffDirection direction = OffDirection.NONE;
            _directionProvider!.directionDict.TryGetValue(_noteCutInfo!.Value, out direction);

            var trueAccuracy = (1f - Mathf.Clamp01(_cutDistanceToCenter / 0.3f)) * 15f;
            _judgmentService.Judge(cutScoreBuffer.noteScoreDefinition, ref _text, ref _color, total, before, after, accuracy, timeDependence, trueAccuracy, direction);
		}
	}
}