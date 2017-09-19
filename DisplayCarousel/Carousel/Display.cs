using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace Carousel
{

    /// <summary>
    /// The Display class will be a base class encapsulating the functionality of all displays which can be in sitelists. 
    /// </summary>
    public abstract class Display : Grid
    {

        #region Members

        // This will be the time the display will show while looping
        public double Timer { get; set; } = 20;

        public string DisplayName { get; set; }

        // This will identify what type of display it is
        public string Type { get; set; }
        
        #endregion


        #region abstract Methods

        // This function should start the setup process for the display. 
        public abstract void Setup();

        // This function should preform the nessisary steps to remove the display from existence. 
        public abstract void Destroy();

        // This function should load the object from a json string defining it. 
        public abstract void load(JObject jsonRepresentation);

        // This function should save the display to a json string it can later parse. 
        public abstract JObject save();
        
        #endregion


        #region Virtual Methods


        // This function is avialable for displays which need an operation to be preformed when they are next up in a looping display. 
        public virtual void UpNext()
        {

        }



        #endregion
        
    }
}
