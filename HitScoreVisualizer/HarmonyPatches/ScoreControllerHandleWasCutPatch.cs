using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HitScoreVisualizer.Models;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using static HitScoreVisualizer.Models.HsvFlyingScoreEffect;

namespace HitScoreVisualizer.HarmonyPatches
{
	internal class ScoreControllerHandleWasCutPatch : IAffinity
	{
		public Dictionary<NoteCutInfo, OffDirection> directionDict = null!;
		private SiraLog _siraLog;

		private static readonly Dictionary<OffDirection, Vector3> _validNormals = new Dictionary<OffDirection, Vector3>
		{
			{ OffDirection.LEFT, new Vector3(-1, 0, 0) },
			{ OffDirection.DOWNLEFT, new Vector3(-Mathf.Sqrt(2)/2, -Mathf.Sqrt(2)/2, 0) },
			{ OffDirection.DOWN, new Vector3(0, -1, 0) },
			{ OffDirection.DOWNRIGHT,new Vector3(Mathf.Sqrt(2)/2, -Mathf.Sqrt(2)/2, 0) },
			{ OffDirection.RIGHT, new Vector3(1, 0, 0) },
			{ OffDirection.UPRIGHT, new Vector3(Mathf.Sqrt(2)/2, Mathf.Sqrt(2)/2, 0) },
			{ OffDirection.UP, new Vector3(0, 1, 0) },
			{ OffDirection.UPLEFT, new Vector3(-Mathf.Sqrt(2)/2, Mathf.Sqrt(2)/2, 0) }
		};

		private static readonly HashSet<OffDirection> _topHalf = new HashSet<OffDirection>
		{
			OffDirection.LEFT,
			OffDirection.DOWNLEFT,
			OffDirection.DOWN,
			OffDirection.DOWNRIGHT
		};

		public ScoreControllerHandleWasCutPatch(SiraLog siraLog)
		{
			directionDict = new Dictionary<NoteCutInfo, OffDirection>();
			_siraLog = siraLog;
		}

		[AffinityPrefix]
		[AffinityPatch(typeof(ScoreController), nameof(ScoreController.HandleNoteWasCut))]
		private void NoteWasCutPrefix(NoteController noteController, in NoteCutInfo noteCutInfo)
		{
			directionDict.Add(noteCutInfo, GetWrongSwingDirection(noteController, in noteCutInfo));
		}

		private OffDirection GetWrongSwingDirection(NoteController noteController, in NoteCutInfo noteCutInfo)
		{
			NoteCutDirection noteCutDirection = noteController.noteData.cutDirection;

			OffDirection closestOD = GetClosestOD(noteCutInfo.cutNormal);

			if (closestOD == OffDirection.NONE)
			{
				return closestOD;
			}

			var normal = _validNormals[closestOD];
			if (Vector3.Dot(normal, noteCutInfo.notePosition - noteCutInfo.cutPoint) > 0)
			{
				return closestOD;
			}
			else
			{
				var castedOD = (int) closestOD;
				if (castedOD < 4)
				{
					return (OffDirection) (castedOD + 4);
				}
				else
				{
					return (OffDirection) (castedOD - 4);
				}
			}
		}

		private OffDirection GetClosestOD(Vector3 cutNormal)
		{
			var closestDot = Mathf.NegativeInfinity;
			OffDirection closestOD = OffDirection.NONE;

			foreach (var direction in _topHalf)
			{
				var normal = _validNormals[direction];
				var dot = Mathf.Abs(Vector3.Dot(cutNormal, normal));
				if (dot > closestDot)
				{
					closestDot = dot;
					closestOD = direction;
				}
			}

			return closestOD;
		}
	}
}
