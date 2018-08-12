namespace ICacheChallenge.Policies
{
    /// <summary>
    /// A flexible eviction policy interface designed for implementation with the 'ICache' interface
    /// </summary>
    public interface ICacheEvictionPolicy
    {
        /// <summary>
        /// Allows tracking of the size of the cache which can be used for all / some eviction polices.
        /// This should be updated as members in the cache change.
        /// </summary>
        int CacheSize { get; set; }

        /// <summary>
        /// Determines if the cache meets the conditions of the policy to have a member evicted.
        /// </summary>
        /// <returns>The result of the determination, if a member is eligble for eviction.</returns>
        bool MemberRequiresEviction();

    }
}
