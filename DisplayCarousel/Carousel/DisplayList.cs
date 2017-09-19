using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Carousel
{
    public class DisplayList
    {

        #region Members 

        // This will be the name of my display list 
        public string Name { get; set; } = "Unnamed";

        // This will be the list of display elements 
        public ObservableCollection<Display> Displays = new ObservableCollection<Display>();

        // This will be the current index I am on 
        public int Index { get; set; } = 0;

        #endregion

        #region State Handling

        internal Display CurrentDisplay()
        {
            return Displays[Index];
        }

        internal Display NextDisplay()
        {
            if (Index == Displays.Count() - 1)
            {
                Index = 0;
                return Displays[Index];
            }
            else
            {
                Index = Index + 1;
                return Displays[Index];
            }
        }

        internal void SetIndex(Display display)
        {
            Index = Displays.IndexOf(display);
        }

        internal Display QuietNextDisplay()
        {
            if (Index == Displays.Count() - 1)
            {
                return Displays[0];
            }
            else
            {
                return Displays[Index + 1];
            }
        }

        #endregion

        #region Saving

        public JObject save()
        {
            JObject SerialList = new JObject();

            JProperty name = new JProperty("Name", Name);
            SerialList.Add(name);

            JArray dislpays = new JArray();
            foreach (Display disp in Displays)
            {
                dislpays.Add(disp.save());
            }
            SerialList.Add(new JProperty("Displays", dislpays));

            return SerialList;

        }


        public void SaveToFile(string filename)
        {
            JObject json = save();

            // If file exists, delete it before writing it out
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
                

            // write it to a file
            System.IO.File.WriteAllText(filename, json.ToString());
        }


        internal DisplayList Load(string filename)
        {

            // Load the json object from the file
            JObject JsonDisplayList = JObject.Parse(File.ReadAllText(filename));

            // Declare my new list
            DisplayList newlist = new DisplayList();

            // Add my inherent properties of the display list
            this.Name = (string)JsonDisplayList.SelectToken("Name");

            // Get my array of displays
            var displays = (JArray)JsonDisplayList.SelectToken("Displays");

            foreach (JObject disp in displays)
            {

                string dispType = (string)disp.SelectToken("Type");

                switch(dispType)
                {
                    case "DisplaySite":
                        Display newDisplay = new Display_Styles.DisplaySite();
                        newDisplay.load(disp);
                        newlist.Displays.Add(newDisplay);
                        break;
                    default:
                        throw new NotImplementedException();
                }

            }


            return newlist;
            
        }

        #endregion

    }
}
