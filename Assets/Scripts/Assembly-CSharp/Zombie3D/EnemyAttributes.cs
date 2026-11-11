namespace Zombie3D
{
	public class EnemyAttributes
	{
		public double Hp;

		public float AttackDamage;

		public float AttackSpeed;

		public float MoveSpeed;

		public int Loot;

		public int ExpLoot;

		// Mod: HP at spawn, used to pay bosses in proportion to their life.
		public double MaxHp;

		// Mod: the original cash value, still used to size survival waves.
		public int SpawnBudget;

		public float attackRange;

		public float detectionRange;
	}
}
