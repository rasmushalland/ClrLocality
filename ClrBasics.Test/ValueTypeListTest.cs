using NUnit.Framework;

namespace ClrBasics.Test
{
	[TestFixture]
	public class ValueTypeListTest : UnitTestBase
	{
		[Test]
		public void Basic()
		{
			var l = new ValueTypeList<int>();
			AreEqual(0, l.Count);

			l.Add(42);
			AreEqual(1, l.Count);
			AreEqualSequences(new[] {42}, l.ToArray());

			l.RemoveAt(0);
			AreEqual(0, l.Count);
		}

		[Test]
		public void Basic2()
		{
			var l = new ValueTypeList<int>(new[] {42, 43});
			AreEqual(2, l.Count);

			AreEqualSequences(new[] {42, 43}, l.ToArray());

			l.RemoveAt(0);
			AreEqual(1, l.Count);
			AreEqualSequences(new[] {43}, l.ToArray());
		}
	}
}