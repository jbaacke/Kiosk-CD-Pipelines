using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp.Wpf;
using CefSharp;
using Newtonsoft.Json.Linq;

namespace Carousel.Display_Styles
{
    class DisplaySite : Display
    {

        // The list of prefixes which are acceptable to navigate to
        public List<string> Prefixes { get; set; } = new List<string>();

        // Wether or not to validate navigation
        public bool IsLockedSite { get; set; }

        // The list of javascript functions designed to let me log into sites or get to the right page
        public List<string> Steps { get; set; } = new List<string>();

        // This is the address of the site
        public string Address { get; set; }

        private ChromiumWebBrowser Browser { get; set; }

        

        public override void load(JObject jsonRepresentation)
        {

            // Set the type
            this.Type = (string)jsonRepresentation.SelectToken("Type");

            // Get the assorted string properties
            this.DisplayName = (string)jsonRepresentation.SelectToken("DisplalyName");
            this.Timer = (double)jsonRepresentation.SelectToken("Timer");
            this.IsLockedSite = (bool)jsonRepresentation.SelectToken("IsLockedSite");
            this.Address = (string)jsonRepresentation.SelectToken("Address");

            // Set up my serializer for the lists


            // Get the prefixes
            var prefs = (JArray)jsonRepresentation.SelectToken("Prefixes");
            foreach (string pref in prefs)
            {
                Prefixes.Add(pref);
            }
            
            // Get the steps
            var steps = (JArray)jsonRepresentation.SelectToken("Steps");
            foreach (string step in steps)
            {
                Steps.Add(step);
            }
            
        }

        public override JObject save()
        {

            JObject SerializedDisplay = new JObject();

            // Add the Type
            SerializedDisplay.Add(new JProperty("Type", "DisplaySite"));

            // Add the assorted string properties
            SerializedDisplay.Add(new JProperty("DisplalyName", DisplayName));
            SerializedDisplay.Add(new JProperty("Timer", Timer));
            SerializedDisplay.Add(new JProperty("IsLockedSite", IsLockedSite));
            SerializedDisplay.Add(new JProperty("Address", Address));

            // Add my Prefixes
            JArray prefs = new JArray();
            foreach(string pref in Prefixes)
            {
                prefs.Add(pref);
            }
            SerializedDisplay.Add(new JProperty("Prefixes", prefs));

            // Add my Steps
            JArray steps = new JArray();
            foreach (string step in Steps)
            {
                steps.Add(step);
            }
            SerializedDisplay.Add(new JProperty("Steps", steps));


            return SerializedDisplay;
        }


        public override void UpNext()
        {
            base.UpNext();
            Browser.Address = Address;
            return;
        }


        public override void Setup()
        {
            CreateLockedBrowser();
        }

        public override void Destroy()
        {

            Browser.Dispose();

            // remove it from the children
            this.Children.Remove(this.Children[0]);

            // Collect the garbage to make sure its gone. 
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        
        public void CreateLockedBrowser()
        {

            Browser = new ChromiumWebBrowser();

            // This request handler checks links before browsing to them to see if they are allowed
            JRequestHandler RequestHandler = new JRequestHandler();
            RequestHandler.Prefixes = Prefixes;
            RequestHandler.Prefixes.Add(Address);
            RequestHandler.IsLockedSite = IsLockedSite;


            // In actual practice this guy needs to be added to those who use him's prefix list.
            RequestHandler.Prefixes.Add("fls.doubleclick.net");

            // Add the steps from the site to the request handler
            RequestHandler.Steps = Steps;


            Browser.RequestHandler = RequestHandler;



            // The lifespan handler handles things like popups. Which don't need top happen.
            Browser.LifeSpanHandler = new JLifeSpanHandler();


            // Make each of these with a new cash path so they don't track eachother
            var requestContextSettings = new RequestContextSettings { CachePath = "" };
            var requestcontext = new RequestContext(requestContextSettings);
            Browser.RequestContext = requestcontext;

            // Navigate to the site and give it an id I can find 
            Browser.Address = Address;
            

            this.Children.Add(Browser);
        }

        
    }
}
