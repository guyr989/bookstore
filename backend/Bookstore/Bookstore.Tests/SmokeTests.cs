using NUnit.Framework;

namespace Bookstore.Tests
{
    // Temporary "smoke test": its only job is to prove the whole test harness
    // (build -> NUnit -> test adapter -> runner) works end to end.
    // We delete this once the first real test exists.
    [TestFixture]
    public class SmokeTests
    {
        [Test]
        public void Harness_Is_Wired_Up()
        {
            Assert.AreEqual(2, 1 + 1);
        }
    }
}
