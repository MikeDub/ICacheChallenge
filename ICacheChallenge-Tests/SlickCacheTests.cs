using System;
using System.Threading;
using ICacheChallenge.Cache;
using ICacheChallenge.Policies;
using NUnit.Framework;

namespace ICacheChallenge.Tests
{
    [TestFixture]
    public class SlickCacheTests
    {
        /// <summary>
        /// Helper-class for testing ICache operations, allows quick and organised field instantiation.
        /// </summary>
        private class CacheTestData
        {
            public string Value = string.Empty;
            public bool GetSuccessful = false;
            public string ExpectedValue = string.Empty;
            public string Errors = string.Empty;
        }

        private ICache<int, string> _cache;
        private ICacheEvictionPolicy _evictionPolicy;

        [SetUp]
        public void Setup()
        {
            //Demonstrated use of the eviction policy
            int maxCacheSize = 3;
            _evictionPolicy = new MaxCacheSizeEvictionPolicy(maxCacheSize);
            _cache = new SlickCache<int, string>(null, _evictionPolicy);
        }

        #region Core Tests

        /// <summary>
        /// Ensure the cache doesn't initialise with any existing memebers / data
        /// </summary>
        [Test]
        public void InitialCacheState_StartsEmpty_AndReturnsNothing()
        {
            //Arrange
            CacheTestData data = new CacheTestData {ExpectedValue = null};

            //Act
            try
            {
                data.GetSuccessful = _cache.TryGetValue(0, out data.Value);
            }
            catch (Exception e)
            {
                data.Errors = e.Message;
            }

            //Assert
            Assert.That(data.Value, Is.EqualTo(data.ExpectedValue), $"Expected value to be null. Error: {data.Errors}.");
            Assert.That(data.GetSuccessful, Is.EqualTo(false));
        }

        /// <summary>
        /// Ensure that when a cache policy is passed in, it prioritises the max size used in the policy,
        /// instead of the default value passed into the cache. 
        /// This is the expected behaviour due to the order of the parameters setup.
        /// </summary>
        [Test]
        public void MaxCacheSize_InheritsFromPolicyWhenSpecified_InsteadOfParameter()
        {
            //Arrange
            CacheTestData[] data = {
                new CacheTestData { ExpectedValue = "0" },
                new CacheTestData { ExpectedValue = "1" }
            };
            _cache = new SlickCache<int, string>(1, _evictionPolicy);

            //Act
            //By adding 2 members, if the cache policy is used, then we will be able to retrieve both members.
            //If the maxSize of 1 paramater specified above is used, the 2nd value will be null.
            AddMembers(2);
            for (int index = 0; index < data.Length; index++)
            {
                CacheTestData test = data[index];
                try
                {
                    test.GetSuccessful = _cache.TryGetValue(index, out test.Value);
                }
                catch (Exception e)
                {
                    test.Errors = e.Message;
                }

                //Assert
                Assert.That(test.Value, Is.EqualTo(test.ExpectedValue), $"Value was not what was expected. Errors: {test.Errors}");
                Assert.That(test.GetSuccessful, Is.EqualTo(true));
            }
        }

        /// <summary>
        /// Tets the Normal Add/Insertion function into the cache
        /// </summary>
        [Test]
        public void AddOrUpdate_InsertedBelowCapacity_SuccessfullyReturnsSameValue()
        {
            //Arrange
            CacheTestData data = new CacheTestData();

            //Act
            AddMembers(2);
            for (int key = 0; key < 2; key++)
            {
                try
                {
                    data.GetSuccessful = _cache.TryGetValue(key, out data.Value);
                    data.ExpectedValue = (key * key).ToString(); //We expect the value to follow this formula used at creation.
                }
                catch (Exception e)
                {
                    data.Errors = e.Message;
                }

                //Assert
                //Test normal below capacity behaviour
                Assert.That(data.GetSuccessful, Is.True, $"Retrieval was not successfull for key: {key}. Due to {data.Errors}.");
                Assert.That(data.Value, Is.EqualTo(data.ExpectedValue));
            }
        }

        /// <summary>
        /// Tests the updating sub-functionality of the AddOrUpdate method, by verifying the result afterwards.
        /// </summary>
        [Test]
        public void AddOrUpdate_SuccessfullyUpdatesCache_ByReturningUpdatedValue()
        {
            //Arrange
            CacheTestData data = new CacheTestData() {ExpectedValue = "2"};

            //Act
            try
            {
                AddMembers(2);
                //Test updating the value of the first key to 2, instead of the default 0.
                _cache.AddOrUpdate(0, "2");
                _cache.TryGetValue(0, out data.Value);
            }
            catch (Exception e)
            {
                data.Errors = e.Message;
            }

            //Assert
            Assert.That(data.Errors, Is.Null.Or.Empty, $"Expected there to be no exceptions during update. Error: {data.Errors}.");
            Assert.That(data.Value, Is.EqualTo(data.ExpectedValue), $"Expected value to be the updated value of {data.ExpectedValue}, not {data.Value}.");
        }

        /// <summary>
        /// Tests the behaviour of the cache retrieve a value outside its acceptable range (value doesn't exist)
        /// </summary>
        [Test]
        public void TryGetValue_WithValueOutOfRange_ReturnsNegativeResult()
        {
            //Arrange
            CacheTestData data = new CacheTestData();

            //Act
            AddMembers(1);
            for (int key = 0; key < 3; key++)
            {
                try
                {
                    data.GetSuccessful = _cache.TryGetValue(key, out data.Value);
                }
                catch (Exception e)
                {
                    data.Errors = e.Message;
                }

                //Assert
                //Test retrieval out of range
                if (key > 1)
                {
                    Assert.That(data.GetSuccessful, Is.False, $"Was not expecting positive result for key: {key}. Due to: {data.Errors}.");
                    Assert.That(data.Value, Is.EqualTo(null));
                }
            }
        }

        /// <summary>
        /// Test retrieval of cache node where it is not the least recent and not the most recent
        /// </summary>
        [Test]
        public void TryGetValue_BetweenMostRecentAndLeastRecent_ReturnsValidValue()
        {
            //Arrange
            CacheTestData data = new CacheTestData();

            //Act
            AddMembers(3);
            try
            {
                data.GetSuccessful = _cache.TryGetValue(1, out data.Value);
            }
            catch (Exception e)
            {
                data.Errors = e.Message;
            }

            //Assert
            Assert.That(data.GetSuccessful, Is.True);
            Assert.That(data.Value, Is.Not.Null.Or.Empty);
        }


        /// <summary>
        /// Test the cache eviction policy works correctly by removing the oldest outside its maximum capacity.
        /// </summary>
        [Test]
        public void AddOrUpdate_EvictsOldestMemberWhenCapacityReached_AndReturnsNothingForFirstInsertedValue()
        {
            //Arrange
            //We are expecting values to be inserted in this order
            //
            // (0, 0) -> (1, 1) -> (2, 4) -> (3, 9)
            //
            //When we retrieve the values however, the first entry should be removed
            CacheTestData[] data =
            { 
                new CacheTestData {ExpectedValue = null},
                new CacheTestData {ExpectedValue = "1"},
                new CacheTestData {ExpectedValue = "4"},
                new CacheTestData {ExpectedValue = "9"},
            };

            //Act
            AddMembers(4);
            
            for (int index = 0; index < 4; index++) //Loop through indexes and assign the outcomes and values
            {
                data[index].GetSuccessful = _cache.TryGetValue(index, out data[index].Value);
            }

            //Assert
            for (int index = 0; index < 4; index++)
            {
                if (index == 0) //First value should be null due to eviction policy
                {
                    Assert.That(data[index].GetSuccessful, Is.False, $"Expected first get result to be false due to being inserted first and removed due to eviction policy.");
                    Assert.That(data[index].ExpectedValue, Is.EqualTo(data[index].Value), $"Expected first value to be null due to being inserted first and removed due to eviction policy.");
                }
                else
                {
                    Assert.That(data[index].GetSuccessful, Is.True, $"Expected second get result to be True.");
                    Assert.That(data[index].ExpectedValue, Is.EqualTo(data[index].Value), $"Expected second get result to be True.");
                }
            }
        }

        /// <summary>
        /// Tests the capacity of the cache to handle 1 million members in an acceptable timeframe
        /// </summary>
        [Test]
        public void TryGetValue_UnderLargeLoads_ReturnsValuesQuickly()
        {
            //Arrange
            DateTime startTime = DateTime.Now;
            CacheTestData data = new CacheTestData();

            //Act
            _cache = new SlickCache<int, string>(1000001);
            AddMembers(1000000);
            for (int key = 0; key < 1000000; key++)
            {
                try
                {
                    data.GetSuccessful = _cache.TryGetValue(key, out data.Value);
                    data.ExpectedValue = (key * key).ToString();
                }
                catch (Exception e)
                {
                    data.Errors = e.Message;
                }
                
                //Assert
                //Test normal below capacity behaviour
                Assert.That(data.GetSuccessful, Is.True, $"Retrieval was not successfull for key: {key}. Due to: {data.Errors}.");
                Assert.That(data.Value, Is.EqualTo(data.ExpectedValue));
            }
            var secs = DateTime.Now.Subtract(startTime).Seconds;
            Assert.That(secs < 20, $"Expected Operation to take < 10 seconds, but took {secs}.");
        }

        /// <summary>
        /// Tests the behaviour of the TryGetValue under threaded conditions
        /// </summary>
        [Test]
        public void TryGetValue_WhenImplementedUnderThreads_IsThreadSafe()
        {
            ExecuteMethod_In_MultipleThreads(Run_GetItem_InLoop);
        }

        /// <summary>
        /// Tests the behaviour of the AddOrUpdate under threaded conditions
        /// </summary>
        [Test]
        public void AddOrUpdate_WhenImplementedUnderThreads_IsThreadSafe()
        {
            ExecuteMethod_In_MultipleThreads(Run_AddItem_InLoop);
        }


        #endregion


        #region Supporting Members

        /// <summary>
        /// Tests running the Get Item cache function under a threaded / looped scenario
        /// </summary>
        /// <param name="keyIndex"></param>
        private void Run_GetItem_InLoop(int keyIndex)
        {
            //Arrange
            bool success = false;
            string expectedValue = (keyIndex * keyIndex).ToString();
            string value = String.Empty;

            try
            {
                //Act
                success = _cache.TryGetValue(keyIndex, out value);
            }
            finally
            {
                //Assert
                Assert.That(success, Is.EqualTo(true));
                Assert.That(value, Is.EqualTo(expectedValue));
            }
        }

        /// <summary>
        /// Tests running the Add Item cache function under a threaded scenario 
        /// </summary>
        /// <param name="keyIndex">The key index passed in by the threading loop.</param>
        private void Run_AddItem_InLoop(int keyIndex)
        {
            //Arrange
            string expectedValue = (keyIndex * keyIndex).ToString();
            string value = expectedValue;

            //Act
            try
            {
                _cache.AddOrUpdate(keyIndex, value);
                _cache.TryGetValue(keyIndex, out value);
            }
            finally
            {
                //Assert
                Assert.That(value, Is.EqualTo(expectedValue));
            }
        }

        /// <summary>
        /// Common Method to execute cache functions under a specific number of threads and loops 
        /// </summary>
        /// <param name="actionToRun">Which cache method to execute</param>
        private void ExecuteMethod_In_MultipleThreads(Action<int> actionToRun)
        {
            int keyCount = 10000;
            int threadCount = 5;
            ThreadPriority threadPriority = ThreadPriority.Highest;

            _cache = new SlickCache<int, string>(keyCount);
            AddMembers(keyCount);

            Thread[] threads = new Thread[threadCount];
            string errors = string.Empty;

            for (int threadNumber = 0; threadNumber < threads.Length; threadNumber++)
            {
                threads[threadNumber] = new Thread(
                    () =>
                    {
                        for (int keyIndex = 0; keyIndex < keyCount; keyIndex++)
                        {
                            actionToRun(keyIndex);
                        }
                    }) { Priority = threadPriority };

                threads[threadNumber].Start();
            }
            Assert.That(errors, Is.Empty, errors);
        }


        /// <summary>
        /// Support method for adding new members to the cache
        /// </summary>
        /// <param name="numberOfMembers">Number of items in the cache you would like to add</param>
        private void AddMembers(int numberOfMembers)
        {

            for (int index = 0; index < numberOfMembers; index++)
            {
                //Stores the value as its key squared
                string value = (index * index).ToString();
                //Add both values to cache
                _cache.AddOrUpdate(index, value);
            }
        }

        #endregion

    }
}
