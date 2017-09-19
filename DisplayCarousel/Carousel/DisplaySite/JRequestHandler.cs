using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;

namespace Carousel
{
    class JRequestHandler : CefSharp.IRequestHandler
    {

        // The list of prefixes which are acceptable to navigate to
        public List<string> Prefixes { get; set; } = new List<string>();

        // Wether or not to validate navigation
        public bool IsLockedSite { get; set; }

        // The list of javascript functions designed to let me log into sites or get to the right page
        public List<string> Steps { get; set; } = new List<string>();

        // Returns true if the application should be allowed to navigate to targetUri
        public bool ValidateNavigation(string targetString)
        {
            Uri targetUri = new Uri(targetString);
            
            if (Prefixes.Count == 0)
            {
                return false;
            }

            // I will check each of the strings in the list of acceptable contained strings
            foreach (string pref in Prefixes)
            {
                if (targetUri.AbsoluteUri.Contains(pref))
                {
                    return false;
                }
            }

            // Otherwise I need to deny the navigation
            return true;
        }




        public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            throw new NotImplementedException();
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return response as IResponseFilter;
        }

        public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
        {
            if (IsLockedSite)
            {
                return ValidateNavigation(request.Url);
            }
            else
            {
                return false;
            }
        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            return CefReturnValue.Continue;
        }

        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            throw new NotImplementedException();
        }

        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            throw new NotImplementedException();
        }

        public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
        {
            throw new NotImplementedException();
        }

        public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
        {
            throw new NotImplementedException();
        }

        public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
        {
            throw new NotImplementedException();
        }

        public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
        {
            //throw new NotImplementedException();
        }

        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {
            //throw new NotImplementedException();
        }

        public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {

            // If I have done all of my steps I can stop
            if (Steps.Count == 0)
            {
                return;
            }

            // Exicute the first step in my list
            if (browserControl.CanExecuteJavascriptInMainFrame)
            {
                try
                {
                    browserControl.ExecuteScriptAsync(Steps.First());
                }
                catch
                {

                }
            }

            // Remove the first step
            //Steps.RemoveAt(0);
        }

        public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
            //throw new NotImplementedException();
        }

        public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            //throw new NotImplementedException();
            return false;
        }

        public bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
