﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestRequest.Builder
{
	internal partial class RequestBuilder
	{
		#region async callback
		internal void BuildCallback()
		{
			if (Context.RequestBody != null && (Context.Method == HttpMethod.Post || Context.Method == HttpMethod.Put))
				Request.BeginGetRequestStream(asyncResult =>
				{
					var request = (HttpWebRequest)asyncResult.AsyncState;

					using (var requestStream = request.EndGetRequestStream(asyncResult))
					{
						var bytes = Context.RequestBody.GetBody();
						requestStream.Write(bytes, 0, bytes.Length);
						requestStream.Close();
					}

					request.BeginGetResponse(GetResponseCallback, request);
				}, Request);
			else
				Request.BeginGetResponse(GetResponseCallback, Request);
		}

		private void GetResponseCallback(IAsyncResult asyncResult)
		{
			try
			{
				var webRequest = (HttpWebRequest)asyncResult.AsyncState;
				HttpWebResponse response;
				try
				{
					response = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);
				}
				catch (WebException ex)
				{
					response = (HttpWebResponse)ex.Response;
					if (response == null)
					{
						Context.FailAction?.Invoke(null, ex.Message);
					}
				}

				if (response != null)
				{
					using (response)
					{
						if (Context.SucceedStatus == response.StatusCode)
						{
							using (var stream = response.GetResponseStream())
							{
								Context.SuccessAction?.Invoke(response.StatusCode, stream);
							}
						}
						else
						{
							using (var stream = response.GetResponseStream())
							{
								if (stream != null)
								{
									using (var reader = new StreamReader(stream))
									{
										Context.FailAction?.Invoke(response.StatusCode, reader.ReadToEnd());
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				Dispose();
			}
		}
		#endregion

		internal async Task WriteRequestBodyAsync()
		{
			var bodyBytes = Context.RequestBody?.GetBody();
			if (bodyBytes == null)
				return;
			Request.ContentLength = bodyBytes.Length;
			using (var requestStream = await Request.GetRequestStreamAsync())
			{
				await requestStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
			}
		}


		internal async Task<HttpWebResponse> GetResponseAsync()
		{
			try
			{
				return (HttpWebResponse)await Request.GetResponseAsync();
			}
			catch (WebException e)
			{
				if (e.Response is HttpWebResponse response)
					return response;
				throw;
			}
		}
	}
}
