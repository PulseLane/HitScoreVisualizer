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

		public override void InitAndPresent(in NoteCutInfo noteCutInfo, int multiplier, float duration, Vector3 targetPos, Quaternion rotation, Color color)
		{
			_noteCutInfo = noteCutInfo;

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
			_saberSwingRatingCounter = noteCutInfo.swingRatingCounter;
			_cutDistanceToCenter = noteCutInfo.cutDistanceToCenter;
			_saberSwingRatingCounter.RegisterDidChangeReceiver(this);
			_saberSwingRatingCounter.RegisterDidFinishReceiver(this);
			_registeredToCallbacks = true;

			if (_configuration == null)
			{
				ScoreModel.RawScoreWithoutMultiplier(_saberSwingRatingCounter, _cutDistanceToCenter, out var beforeCutRawScore, out var afterCutRawScore, out var cutDistanceRawScore);
				_text.text = GetScoreText(beforeCutRawScore + afterCutRawScore);
				_maxCutDistanceScoreIndicator.enabled = cutDistanceRawScore == 15;
				_colorAMultiplier = beforeCutRawScore + afterCutRawScore > 103.5 ? 1f : 0.3f;
			}
			else
			{
				_maxCutDistanceScoreIndicator.enabled = false;

				// Apply judgments a total of twice - once when the effect is created, once when it finishes.
				Judge(_noteCutInfo.Value.swingRatingCounter, 30);
			}

			InitAndPresent(duration, targetPos, rotation, false);
		}

		protected override void ManualUpdate(float t)
		{
			var color = _color.ColorWithAlpha(_fadeAnimationCurve.Evaluate(t));
			_text.color = color;
			_maxCutDistanceScoreIndicator.color = color;
		}

		public override void HandleSaberSwingRatingCounterDidChange(ISaberSwingRatingCounter swingRatingCounter, float rating)
		{
			if (_configuration == null)
			{
				base.HandleSaberSwingRatingCounterDidChange(swingRatingCounter, rating);
				return;
			}

			if (_configuration.DoIntermediateUpdates)
			{
				Judge(swingRatingCounter);
			}
		}

		public override void HandleSaberSwingRatingCounterDidFinish(ISaberSwingRatingCounter saberSwingRatingCounter)
		{
			if (_configuration != null)
			{
				Judge(saberSwingRatingCounter);
			}

			base.HandleSaberSwingRatingCounterDidFinish(saberSwingRatingCounter);
		}

		private void Judge(ISaberSwingRatingCounter swingRatingCounter, int? assumedAfterCutScore = null)
		{
			ScoreModel.RawScoreWithoutMultiplier(swingRatingCounter, _cutDistanceToCenter, out var before, out var after, out var accuracy);
			after = assumedAfterCutScore ?? after;
			var total = before + after + accuracy;
			var timeDependence = Mathf.Abs(_noteCutInfo!.Value.cutNormal.z);

			OffDirection direction = OffDirection.NONE;
			_directionProvider!.directionDict.TryGetValue(_noteCutInfo!.Value, out direction);

			var trueAccuracy = (1f - Mathf.Clamp01(_cutDistanceToCenter / 0.3f)) * 15f;

			_judgmentService.Judge(ref _text, ref _color, total, before, after, accuracy, timeDependence, trueAccuracy, direction);
		}
	}

	internal enum OffDirection
	{
		NONE,
		UNDER,
		OVER
	}
}