using System.Threading;
using NUnit.Framework;

namespace FooLib.Test
{
    [TestFixture]
    public class DoerTest
    {
        [Test]
        public void Test_Add_2_3()
        {
            Thread.Sleep(100);

            // act
            var result = Doer.Add(2, 3);

            // assert
            Assert.That(result, Is.EqualTo(5));
        }

        [NUnit.Framework.Test]
        public void Test_Add_6_3()
        {
            // act
            var result = Doer.Add(6, 3);

            // assert
            Assert.That(result, Is.EqualTo(9));
        }

        [Test]
        public void Test_Exception()
        {
            // arrange
            int? sth = null;

            // act
            var result = Doer.Add(2, sth.Value);

            // assert
            Assert.That(result, Is.EqualTo(7));
        }

        private void Helper()
        {
        }
    }
}
