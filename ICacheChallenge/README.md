Slick Cache (ICacheChallenge)
=========

In-memory, least recently used cache inspired by Fone Dynamics which implements the ICache<TKey, TValue> interface.


### Motivation

A Cache designed to handle frequently accessed data in a large database.
It was decided that an in-memory cache within the application itself would help speed up the application and reduce load on the database. 
Methods have been implements in a thread safe manner to handle large volumes of transactions.

### Usage

```
int maxCapacity = 2;
SlickCache cache = new SlickCache<int, string>(maxCapacity);

// Can be any type of key / value specified when constructing the cache
int index = 1;
string value = "A"; 

// To Insert a value / or update in the cache:
cache.AddOrUpdate(index, value);

// To retrieve a value from the cache:
// Define variable to store value to get from the cache
string value; 

// The retrieval method returns if it was successful or not and outputs the value into the output parameter.
bool getSuccessful = cache.TryGetValue(0, out value);

```


### Fields / Properties

The following fields / properties are used in the underlying functionality of the Cache:

- `maxCapacity`: The maximum size of the cache, passed into the constructor. If this capacity is reached, it removes the oldest member (current eviction policy).
- `Exceptions`: Keeps track of any exceptions encountered while performing operations. For the moment this is just implemented as a single / line-broken string.


### Future enhancements

- Implement tests to test out multi-threading / spinlock functionality
- Instead of passing in maxCapacity to the cache constructor, introduce a ICacheEvictionPolicy which takes this argument.
  This will allow greater flexibility in the event the policy is slightly modified,
  by allowing different policies to be introduced without touching the cache.