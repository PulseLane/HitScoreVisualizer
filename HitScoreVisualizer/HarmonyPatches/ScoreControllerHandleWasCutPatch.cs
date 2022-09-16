using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HitScoreVisualizer.Models;
using SiraUtil.Affinity;
using static HitScoreVisualizer.Models.HsvFlyingScoreEffect;

namespace HitScoreVisualizer.HarmonyPatches
{
	internal class ScoreControllerHandleWasCutPatch : IAffinity
	{
		public Dictionary<NoteCutInfo, OffDirection> directionDict = null!;

		public ScoreControllerHandleWasCutPatch()
		{
			directionDict = new Dictionary<NoteCutInfo, OffDirection>();
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

			if (noteCutDirection == NoteCutDirection.Left || noteCutDirection == NoteCutDirection.Right)
			{
				return noteController.noteTransform.position.y <= noteCutInfo.cutPoint.y ? OffDirection.OVER : OffDirection.UNDER;
			}
			else
			{
				return noteController.noteTransform.position.x >= noteCutInfo.cutPoint.x ? OffDirection.UNDER : OffDirection.OVER;
			}
		}
	}
}
