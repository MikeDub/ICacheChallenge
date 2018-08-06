using NUnit.Framework;

namespace ICacheChallenge.Tests
{
    [TestFixture]
    public class SlickCacheTests
    {
        private SlickCache<int, string> _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SlickCache<int, string>(2);
        }

        /// <summary>
        /// Supports adding new members to the cache upto a maximum of 26 members
        /// </summary>
        /// <param name="numberOfMembers">Number of items in the cache you would like to add</param>
        private void AddMembers(int numberOfMembers)
        {
            var modulo = numberOfMembers % 26;

            //Don't allow characters to go outside the alphabet range
            numberOfMembers = numberOfMembers > 26 ? 26 : numberOfMembers;

            for (int index = 0; index < numberOfMembers; index++)
            {
                //Converts the index number to a alphabetical counterpart 
                char cValue = (char) (index + 65);
                string value = cValue.ToString();
                //Add both values to cache
                _cache.AddOrUpdate(index, value);
            }
        }

        [Test]
        public void TestCache_StartsEmpty()
        {
            //Arrange
            string value = string.Empty;
            bool getSuccessful = false;

            //Act
            getSuccessful = _cache.TryGetValue(0, out value);

            //Assert
            Assert.That(value, Is.EqualTo(null));
            Assert.That(getSuccessful, Is.EqualTo(false));
        }

        [Test]
        public void TestInsert_BelowCapacity()
        {
            //Arrange
            string value = string.Empty;
            bool getSuccessful = false;

            //Act
            AddMembers(2);
            for (int key = 0; key < 2; key++)
            {
                getSuccessful = _cache.TryGetValue(key, out value);
                int cCode = 'A' + key;
                var expectedValue = ((char)cCode).ToString();

                //Assert
                //Test normal below capacity behaviour
                Assert.That(getSuccessful, Is.True, $"Retrieval was not successfull for key: {key}. Due to: {_cache.Exceptions}.");
                Assert.That(value, Is.EqualTo(expectedValue));
            }
        }

        [Test]
        public void Test_GetItem_OutOfRange()
        {
            //Arrange
            string value = string.Empty;
            bool getSuccessful = false;

            //Act
            AddMembers(1);
            for (int key = 0; key < 2; key++)
            {
                getSuccessful = _cache.TryGetValue(key, out value);

                //Assert
                //Test retrieval out of range
                if (key > 0)
                {
                    Assert.That(getSuccessful, Is.False, $"Was not expecting positive result for key: {key}. Due to: {_cache.Exceptions}.");
                    Assert.That(value, Is.EqualTo(null));
                }
            }
        }

        [Test]
        public void Test_CapacityReached_And_OldestRemoved_And_MostRecent_Updated()
        {
            //Arrange
            string value = string.Empty;
            bool getOneSuccessful = false;
            bool getTwoSuccessful = false;
            bool getThreeSuccessful = false;

            //We are expecting values to be inserted in this order
            //0, A
            //1, B
            //2, C

            //When we retrieve the values however, the first entry should be removed
            string expectedFirstEntry = null;
            string expectedSecondEntry = "B";
            string expectedThirdEntry = "C";

            string actualFirstValue = string.Empty;
            string actualSecondValue = string.Empty;
            string actualThirdValue = string.Empty;

            //Act
            AddMembers(3);

            getOneSuccessful = _cache.TryGetValue(0, out actualFirstValue);
            getTwoSuccessful = _cache.TryGetValue(1, out actualSecondValue);
            getThreeSuccessful = _cache.TryGetValue(2, out actualThirdValue);

            //Assert
            Assert.That(getOneSuccessful, Is.False, $"Expected first get result to be false due to being inserted first and removed due to eviction policy.");
            Assert.That(getTwoSuccessful, Is.True, $"Expected second get result to be True.");
            Assert.That(getThreeSuccessful, Is.True, $"Expected third get result to be True.");

            //Is this correct - confirm? ? ? 

            Assert.That(expectedFirstEntry, Is.EqualTo(actualFirstValue), $"Expected first value to be null due to being inserted first and removed due to eviction policy.");
            Assert.That(expectedSecondEntry, Is.EqualTo(actualSecondValue), $"Expected first value to equal: {expectedSecondEntry}.");
            Assert.That(expectedThirdEntry, Is.EqualTo(actualThirdValue), $"Expected first value to equal: {expectedThirdEntry}.");
        }

        /// <summary>
        /// TODO
        /// </summary>
        [Test]
        public void Test_GetItem_IsThreadSafe()
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        [Test]
        public void Test_InsertItem_IsThreadSafe()
        {
        }
    }
}
