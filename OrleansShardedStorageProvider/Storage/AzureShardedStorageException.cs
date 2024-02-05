namespace OrleansShardedStorageProvider.Storage
{
	/// <summary>
	/// Exception for throwing from Redis grain storage.
	/// </summary>
	[GenerateSerializer]
	public class AzureShardedStorageException : Exception
	{
		/// <summary>
		/// Initializes a new instance of <see cref="RedisStorageException"/>.
		/// </summary>
		public AzureShardedStorageException()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="RedisStorageException"/>.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public AzureShardedStorageException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="RedisStorageException"/>.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public AzureShardedStorageException(string message, Exception inner) : base(message, inner)
		{
		}


	}
}
