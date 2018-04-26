using System;
using System.IO;
using Retrofit.Converter;

namespace Retrofit
{
	public class RetrofitError : Exception
	{
		private Converter.Converter _converter;
		private Kind _kind;
		private string _response;
	    private Type _successType;
	    private string _url;

	    public Kind Kind
        {
            get
            {
                return _kind;
            }
        }

        public string Response
        {
            get
            {
                return _response;
            }
        }

        public string Url
        {
            get
            {
                return _url;
            }
        }

        public RetrofitError(string message, string url, string response, Converter.Converter converter,
			Type successType, Kind kind, Exception exception) : base(message, exception)
		{
			this._url = url;
			this._response = response;
			this._converter = converter;
			this._successType = successType;
			this._kind = kind;
		}

		public static RetrofitError NetworkError(string url, IOException exception)
		{
			return new RetrofitError(exception.Message, url, "", null, null, Kind.NETWORK,
				exception);
		}

		public static RetrofitError ConversionError(string url, string response, Converter.Converter converter,
			Type successType, ConversionException exception)
		{
			return new RetrofitError(exception.Message, url, response, converter, successType,
				Kind.CONVERSION, exception);
		}

		public static RetrofitError HttpError(string url, string response, Converter.Converter converter,
			Type successType)
		{
			var message = response;
//				string message = response.getStatus() + " " + response.getReason();
			return new RetrofitError(message, url, response, converter, successType, Kind.HTTP, null);
		}

		public static RetrofitError UnexpectedError(string url, Exception exception)
		{
			return new RetrofitError(exception.Message, url, "", null, null, Kind.UNEXPECTED,
				exception);
		}
	}

	/** Identifies the event kind which triggered a {@link RetrofitError}. */
	public enum Kind
	{
		/** An {@link IOException} occurred while communicating to the server. */
		NETWORK,

		/** An exception was thrown while (de)serializing a body. */
		CONVERSION,

		/** A non-200 HTTP status code was received from the server. */
		HTTP,

		/**
		 * An internal error occurred while attempting to execute a request. It is best practice to
		 * re-throw this exception so your application crashes.
		 */
		UNEXPECTED
	}
}