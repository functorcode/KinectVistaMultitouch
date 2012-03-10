using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NITEProvider
{
    class OpenPalm 
    {
        /// <summary>
        /// class constructor
        /// </summary>
        /// <param name="key"> string (key) that will be used to identify the gesture </param>
         int openpalmcounter = 0;
        int closepalmcounter = 0;
        bool prev_status1 = true;
        public OpenPalm()
        {

        }
        
        /// <summary>
        /// this routine checks if the hand is open or not. Openness of the palm is currently indicated
        /// by at least 3 visible fingers sticking out of the profile of the hand.
        /// </summary>
        /// <param name="clipping"></param>
        /// <param name="handPoint"></param>
        /// <returns></returns>
        public  int isDetected( byte[,] clipping, OpenNI.Point3D handPoint)
        {
            /*Since this class is a child of the HandFeature class, we can directly invoke the hand feature extraction routines from 
             the parent class. The result of the parent class is stored in its data structures viz. fingerTips. This list of points is 
             accessible using a getter.
             
             if the number of fingers clearly detected is greater that 3, indicate that the palm has been opened 
             */
            HandFeature featuer = new HandFeature();
            int status = featuer.isDetected(ref clipping, handPoint);
          return status;
  
        }



   
    }
    
}
