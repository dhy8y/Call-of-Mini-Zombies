using UnityEngine;

namespace Zombie3D
{
	public class DeadState : EnemyState
	{
		public override void NextState(Enemy enemy, float deltaTime, Player player)
		{
			if (Time.time - startTime >= ModTuning.CorpseListRemovalTime)
			{
				enemy.RemoveDeadbodyAfterEffect();
			}
		}

		public override void OnHit(Enemy enemy)
		{
		}
	}
}
