using System.Diagnostics;
using System.Net;

if (args.Length == 0)
{
	Console.WriteLine("Usage: fredir <url>");
	return;
}

var url = args[0];
var lastUrl = url;
var redirects = 0;

if (!url.StartsWith("http://") && !url.StartsWith("https://"))
{
	url = "https://" + url;
	lastUrl = url;
}

Console.WriteLine(url);

using var handler = new HttpClientHandler { AllowAutoRedirect = false };
using var client = new HttpClient(handler);

var stopwatch = Stopwatch.StartNew();
var stop = true;

try
{
	while (stop)
    {
    	using var message = new HttpRequestMessage(HttpMethod.Head, lastUrl);
    	var response = await client.SendAsync(message);
    
    	switch (response.StatusCode)
    	{
    		case HttpStatusCode.OK:
    			if (lastUrl == url)
    			{
    				Console.WriteLine("-> direct");
    				return;
    			}
    			
    			stop = false;
    			break;
    		case HttpStatusCode.Redirect:
    		case HttpStatusCode.MovedPermanently:
    		case HttpStatusCode.RedirectKeepVerb:
    		case HttpStatusCode.RedirectMethod:
    			var redirectUrl = response.Headers.Location?.ToString();
    			if (string.IsNullOrEmpty(redirectUrl))
    			{
    				Console.WriteLine("-> unknown location");
    				return;
    			}
    			
    			if (!redirectUrl.Contains("://"))
    			{
    				redirectUrl = new Uri(new Uri(url), redirectUrl).ToString();
    			}
    
    			if (lastUrl == redirectUrl)
    			{
    				Console.WriteLine("-> redirect loop");
    				stop = false;
    				break;
    			}
    			
    			lastUrl = redirectUrl;
    			Console.WriteLine($"-> {redirectUrl}");
    			redirects++;
    			break;
    		default:
    			Console.WriteLine($"-> {response.StatusCode}");
    			return;
    	}
    }
}
catch (HttpRequestException e)
{
	if (e.StatusCode is null or HttpStatusCode.NotFound)
	{
		Console.WriteLine("-> not found");
		return;
	}
	
	Console.WriteLine($"-> {e.StatusCode}");
	return;
}

stopwatch.Stop();
Console.WriteLine($"Redirected {redirects} times to {lastUrl} in {stopwatch.ElapsedMilliseconds}ms");