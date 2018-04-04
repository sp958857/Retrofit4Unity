#region License
// Author: Weichao Wang     
// Start Date: 2017-05-24
#endregion
namespace Retrofit
{
    public interface RequestInterceptor
    {
        void Intercept(object request);
    }
	public class DefaultRequestInterceptor : RequestInterceptor
	{
		public void Intercept(object request)
		{
			//do nothing
		}
	}
}