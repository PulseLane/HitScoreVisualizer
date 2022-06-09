using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HitScoreVisualizer.Models;
using SiraUtil.Affinity;

namespace HitScoreVisualizer.HarmonyPatches
{
	internal class NoteCutSpawnerPatch : IAffinity
	{
		public Dictionary<NoteCutInfo, OffDirection> directionDict = null!;

		public NoteCutSpawnerPatch()
		{
			directionDict = new Dictionary<NoteCutInfo, OffDirection>();
		}

		[AffinityPrefix]
		[AffinityPatch(typeof(NoteCutScoreSpawner), nameof(NoteCutScoreSpawner.HandleNoteWasCut))]
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
