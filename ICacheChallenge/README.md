SlickCache (ICache<TKey, TValue>)
=========

In-memory, least recently used cache which implements the `ICache<TKey, TValue>` interface.
Unit tests have been provided in a seperate project with 100% coverage, all passing.

### Motivation

A Cache designed to handle frequently accessed data in a large database.
It was decided that an in-memory cache within the application itself would help speed up the application and reduce load on the database. 
Methods have been implemented in a thread safe manner to handle large volumes of transactions.

### Usage
For all demonstrations, we will assume the use of the `int` type for **TKey** and `string` type for **TValue**.

+ **Cache Instantiation**:
```
// Simple Instantiation
int maxCacheSize = 3;
ICache<int, string> cache = new SlickCache<int, string>(maxCacheSize);

// Instantiation with a specified eviction policy
ICacheEvictionPolicy evictionPolicy = new MaxCacheSizeEvictionPolicy(maxCacheSize);
ICache<int, string> cache = new SlickCache<int, string>(null, evictionPolicy);

// If you use both, the eviction policy configuration will take priority
ICache<int, string> cache = new SlickCache<int, string>(2, evictionPolicy);
```
The above code will still result in the Cache having a **maxSize** of 3, if the eviction policy is not null. 
However, if the policy was null or not able to be resolved, it would fallback to the parameter -> **maxSize** = 2.

+ **Adding or Updating a Value in the Cache**:
```
// Define a key and value you wish to store in the cache
int index = 1;
string value = "A"; 

// To Insert a value / or update in the cache:
cache.AddOrUpdate(index, value);
```
If the key exists in the cache, it will simply be updated with the new value.
However, if the key doesn't exist the new key/value pair will be inserted, and the least recently used / most recently used values will be updated.

+ **Retrieving a Value from the Cache**:
```
int key = 1;
// To retrieve a value from the cache, define variable to store value to get from the cache
string value = String.Empty; 

// The retrieval method returns if it was successful or not and outputs the value into the output parameter.
bool getSuccessful = cache.TryGetValue(key, out value);
```

### Fields / Properties

The following fields / properties are used in the underlying functionality of the Cache:

- `maxSize`: The maximum size of the cache passed into the constructor. If this capacity is reached, it removes the oldest member (current eviction policy).
- `evictionPolicy`: The eviction policy used in the cache. This policy can be changed at any time by passing in a different eviction policy. By Default, the MaxCacheSizeEvictionPolicy is used with the maxSize parameter passed into the cache constructor. This max size can also be overridden by passing in an instance to the cache constructor with a different max size.


### Future Considerations

If there comes a time where the eviction policy size grows or additional evicition policies are introduced, policy tests should be moved to a seperate file.