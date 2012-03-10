using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.Features2D;
using System.Drawing;
using OpenNI;

namespace NITEProvider
{
    class HandFeature 
    {

        public static bool isDone;                                  //flag to represent status of detection
                

        /// <summary>
        /// class constructor
        /// </summary>
        public HandFeature()
        {
                   
        }

        /// <summary>
        /// Parameterized class constructor
        /// </summary>
        /// <param name="key">the string that will be used to identify this class object</param>
 
        /// <summary>
        /// set method for flag 'isDone'
        /// </summary>
     
        /// <summary>
        /// getter for the fingerTips array list
        /// </summary>
        /// <returns>returns a list containing the number of finger tips detected</returns>
      
       
        
        /// <summary>
        /// invoked by GestureManager to check detection
        /// </summary>
        /// <param name="clipping">clipping of the depth map containing hand point</param>
        /// <returns>true / false if gesture is detected</returns>
        public  int isDetected(ref byte[,] clipping, Point3D handPoint)
        {
         
            isDone = false;

            int size = (int)Math.Sqrt(clipping.Length);                         //get the size of the clip
            int center = size / 2;                                              //get the center 
            Contour<Point> hand = extract_hand(clipping);                       //extract the hand contour from this clip
            MemStorage st = new MemStorage(); 
            MemStorage cmst=new MemStorage();//general purpose storage
       
          
            if (hand != null)                                                   //if hand contour extracted was not null, do this
            {
                /*Step 1 - Distance calculation of all contour points from center*/

                Double[] dis = new Double[hand.Total];                          //this array will store the distance from the center of all the contour points
                for (int i = 0; i < hand.Total; i++)                            //this loop will calculate those distances
                {
                    Point pt = new Point();
                    pt.X = hand[i].X - center;
                    pt.Y = hand[i].Y - center;
                    double d = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);            //calculating distance of a contour point from center
                    dis[i] = d;                                                 //store this value in the array    
                }

                /*Step 2 - determining local maxima and minima points*/
                
              int[] max = new int[20];                                        //this array will store the local maxima points
                int[] min = new int[20];                                        //this array will store the local minima points
                int mincount = 0;                                               //total number of local minima points        
                int maxcount = 0;                                               //total number of local maxima points    

                find_peak(dis, 5, ref min, ref max, ref mincount, ref maxcount);//find the local maxima and minima in the clip and store them in the vars declared above
               if (mincount <=3 && maxcount <=3)
                {
                    return 2; //close hand
                }
                else
                {
                    return 1; //open hand
                }
           
              

          }

            return 0; //unknown

        }

        /// <summary>
        /// this routine finds out the local maxima and local minima points from the contour. 
        /// </summary>
        /// <param name="dis">array containing the distances of each contour point from center of clipping</param>
        /// <param name="delta">minimum distance (threshold) between consecutive maxima and minima points</param>
        /// <param name="min">array in which the minima points will be stored</param>
        /// <param name="max">array in which the maxima points will be stored</param>
        /// <param name="mincount">the count of the number of minima points will be stored here</param>
        /// <param name="maxcount">the count of the number of maxima points will be stored here</param>
        private void find_peak(Double[] dis, double delta, ref int[] min, ref int[] max, ref int mincount, ref int maxcount)
        {
            //intialize the two arrays with 0
            for (int i = 0; i < 20; i++)
            {
                max[i] = 0;
                min[i] = 0;
            }

            int mnpos = 0;                                                          //the index of the currently assumed minima point in the distance array
            int mxpos = 0;                                                          //the index of the currently assumed maxima point in the distance array
            double mx = -1000.0;                                                    //assumption for negative infinity - this is for the very first maxima comparison    
            double mn = 1000.0;                                                     //assumption for positive infinity - this is for the very first minima comparison
            int lookformax = 1;                                                     //flag to look for maxima or minima point. if true(1) then it looks for maxima points
            int mx_count = 1;                                                       //number of maxima points detected
            int min_count = 1;                                                      //number of minima points detected
            
            for (int i = 1; i < dis.Length; i++)
            {
                /*The following two if blocks compare the current countour point's distance against the reference values (mx and mn). Note that for the very first
                 comparison, these are set to -infinity and +infinity. The first comparison always yields true for both if clauses and hence we establish the first 
                 distance point to be our reference for every successive comparison.
                 
                 The value of 'mx' keeps changing until a local maxima is found, after which the look for maxima flag is toggled and the loop looks only for local minima.
                 While searching for local minima, mx stays the same, and 'mn' is constantly changed. After a local minima point is located, the process continues till the
                 entire distance array is scanned.
                 */
                
                if (dis[i] > mx)                                                    
                {
                    mx = dis[i];
                    mxpos = i;
                }

                if (dis[i] < mn)
                {
                    mn = dis[i];
                    mnpos = i;
                }

                if (lookformax == 1)
                {
                    //condition to identify a local maxima point
                    if (dis[i] < mx - delta)            
                    {
                        //if the resultant array is not completely filled up, save this point
                        if (mx_count < 20)
                            max[mx_count] = mxpos;
                        
                        mn = dis[i];                //make this the minima point for next comparison
                        mnpos = i;                  //save the index of this point    
                        lookformax = 0;             //tell the loop to start looking for minima points now
                        mx_count = mx_count + 1;    //increment the count of maxima points
                    }
                }
                else
                {
                    //condition to identify a local minima point
                    if (dis[i] > mn + delta)
                    {
                        //if the resultant array is not completely filled up, save this point
                        if (min_count < 20)
                            min[min_count] = mnpos; //save this minima point
                        
                        mx = dis[i];                //this distance value will become ref for next maxima comparison    
                        mxpos = i;                  //save the index of this point
                        lookformax = 1;             //now look for maxima points
                        min_count = min_count + 1;  //increment the count of minima points
                    }
                }
            }

            mincount = min_count;                   //save the count of minima points in the argument that was passed by reference
            maxcount = mx_count;                    //save the count of maxima points in the argument that was passed by reference
        }

  
        private Contour<Point> extract_hand(byte[,] clipping)
        {
            int size = (int)Math.Sqrt(clipping.Length);                         //calculate the size of the clip
            MemStorage storage = new MemStorage();                              //general storage for CV
            Image<Gray, byte> image = new Image<Gray, byte>(size, size);        //this object is used to convert the clip into an image

            for (int i = 0; i < size; i++)                                      //this loop manages the conversion (clip is binary double dim array - wont work with CV's contour api)
            {
                for (int j = 0; j < size; j++)
                    image[j, i] = new Gray(clipping[i, j]);
            }

            Contour<Point> contour = image.FindContours(                        //find the countours using openCV for this image     
                        Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE,
                        Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);

            /* the following loop scans through all the contours returned, and picks out the one whose size is greater than 100 sequence points */
            try
            {

                /*  do
                  {
                      if (contour.Total < 100)                                        //check the size of the contour
                          contour = contour.HNext;                                    //if its less than hundred move to the next contour
                      else break;                                                     //a contour of size > 100 is found, stop looking                
                  }
                  while (contour.HNext != null);*/
                int center = size / 2;
                /*we've now identified a contour, but we must ensure that this hand contour contains the hand point. This loop checks for exactly that*/
                for (Contour<Point> ptr = contour; ptr != null; ptr = ptr.HNext)
                {
                    Rectangle rect = ptr.BoundingRectangle;
                    if (rect.Contains(new Point(size / 2, size / 2)))
                    {
                        int dis = ((size / 2) - rect.Y);
                        int maxlimit = (rect.Y + 2 * dis);
                        for (int i = 0; i < ptr.Total; i++)
                        {
                            if (ptr[i].Y > maxlimit)
                            {
                                ptr.RemoveAt(i);
                                if (i > 0)
                                {
                                    i--;
                                }
                            }

                        }

                        int min = size;
                        int minindex = 0;
                        int maxindex = 0;
                        int max = 0;

                        for (int i = 0; i < ptr.Total; i++)
                        {
                            if (ptr[i].Y == maxlimit)
                            {
                                if (ptr[i].X < min)
                                {
                                    min = ptr[i].X;
                                    minindex = i;
                                }
                                if (ptr[i].X > max)
                                {
                                    max = ptr[i].X;
                                    maxindex = i;
                                }
                            }
                        }
                
                        for (int j = 0; j < size; j++)
                        {
                            for (int i = 0; i < size; i++)
                            {
                                if (i > maxlimit)
                                {
                                    image[i, j] = new Gray(0);
                                }
                                else if (i == maxlimit && j > min && j < max)
                                {
                                    image[maxlimit, j] = new Gray(255);
                                }
                            }

                        }

                        Contour<Point> newc = image.FindContours(                        //find the countours using openCV for this image     
                                    Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE,
                                    Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
                        for (Contour<Point> newptr = newc; newptr != null; newptr = newptr.HNext)
                        {
                            Rectangle newrect = newptr.BoundingRectangle;
                            if (newrect.Contains(new Point(size / 2, size / 2)))
                            {
                                return newptr;
                            }
                        }

                        /*   for (int i = minindex; i < maxindex ; i++)
                           {
                               ptr.Insert(i, new Point(min + counter, maxlimit));
                               counter++;
                           }*/
                        /*      Image<Rgb, byte> image1 = new Image<Rgb, byte>(200, 200);
                              image1[center, center] = new Rgb(0, 255, 0);
                     
                            //  CvInvoke.cvEllipseBox(image1.Ptr, ellipse.MCvBox2D,new MCvScalar(255,0,0), 1, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
                      
                     
                             for (int i = 0; i < ptr.Total; i++)
                              {
                                  if (ptr[i].Y > (rect.Y + 2*dis))
                                  {
                                      ptr.RemoveAt(i);
                                      if (i > 0)
                                      {
                                          i--;
                                      }
                                  }
                          
                              }
                             for (int i = 0; i < ptr.Total; i++)
                             {
                                   image1[ptr[i].Y, ptr[i].X] = new Rgb(0, 255, 0);
                         

                             }
                             /* for (int j = 0; j < size; j++)
                              {
                                  for (int i = 0; i < size; i++)
                                  {
                                      if (i > dis)
                                      {
                                          image[i, j] = new Gray(0);
                                      }
                                  }
                              }*/

                        //  CvInvoke.cvShowImage("Test Window", image.Ptr); 
                        //the hand point lies at the center of the clipping matrix. Hence are its coords (size/2,size/2) relative to [0,0] of the clip
                        //  return ptr;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            };

            return null;
        }

     
     

    }
}
