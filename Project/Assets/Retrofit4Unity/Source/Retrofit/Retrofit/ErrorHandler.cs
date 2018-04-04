using System;

namespace Retrofit
{
	public interface ErrorHandler
	{
		Exception handleError(RetrofitError cause);
	}
	public class DefaultErrorHandler : ErrorHandler {
		public Exception handleError(RetrofitError cause)
		{
			return cause;
		}
	}
}
