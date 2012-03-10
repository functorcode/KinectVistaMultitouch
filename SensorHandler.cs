using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using OpenNI;
using NITE;


namespace NITEProvider
{
    class SensorHandler
    {

     System.Timers.Timer timer;
        int count = 0;
        Context cxt;                                        //context - this will read data from the sensor
        DepthGenerator depthGen;                            //depth generator object, will target the depth node of the sensor
        DepthMetaData depthMeta;                            //this will contain the depth map data
        int scrWidth;
        int scrHeight;
        const int MAX_DEPTH = 5000;                         //this is the maximum distance that will be sensed
        byte[,] clipping;                                   //this double dim array will store the clipping around the hand point
        const int clipSize = 200;
        const int MAX_ALLOWED_HAND_DEPTH = 1000;
        public int xRes, yRes;                               //the max resolution supported by sensor    
        double mouseSpeed;
        SessionManager sessionMgr;                          //session manager object
        PointControl pointCtrl;                             //this will track the point
        PushDetector pushDetect;
        WaveDetector waveDetect;
        HandsGenerator gsHandsGenerator;
        SteadyDetector steadydetector;
        Thread updateThread;                                //this thread will update the context and session manager
        ArrayList HandPointBuffer;
        PointCollection pointCollections;
        bool isActive;                                      //this flag controls the lifecycle of the thread
        Point3D handPoint;  
        int frameCounter = 0;
        FlowRouter flrouter;
        Broadcaster brodcaster;
        readonly InputProvider inputProvider;
        OpenPalm openpalm;
        Point[] pbuffer;
      
        int point_counter = 0;
        int change_point = 0;
        int is_push_allow = 0;
            int is_push_allow_counter=0;
            int is_steady = 0;
          
        public SensorHandler(InputProvider inputProvider)
		{
			this.inputProvider = inputProvider;
		}
      
        public bool initializeSensor(String xmlPath)
        {
            try

            {
              
                pbuffer =new  Point[6];
                openpalm = new OpenPalm();
                scrHeight = SystemInformation.PrimaryMonitorSize.Height;
                scrWidth = SystemInformation.PrimaryMonitorSize.Width;
               
                mouseSpeed = SystemInformation.MouseSpeed * 0.15;
                pointCollections = new PointCollection();
                /*OpenNI objects - Context, DepthGenerator and DepthMetaData are initialized here*/
                cxt = new Context(xmlPath);
                depthGen = cxt.FindExistingNode(NodeType.Depth) as DepthGenerator;
                gsHandsGenerator = cxt.FindExistingNode(NodeType.Hands) as HandsGenerator;
                gsHandsGenerator.SetSmoothing(0.1f);
                depthMeta = new DepthMetaData();
                if (depthGen == null) return false;

                xRes = depthGen.MapOutputMode.XRes;
                yRes = depthGen.MapOutputMode.YRes;

                /*NITE objects - Session manager, PointControl is initialized here*/
                sessionMgr = new SessionManager(cxt, "Wave", "RaiseHand");

                pointCtrl = new PointControl("PointTracker");
                steadydetector = new SteadyDetector();
                flrouter = new FlowRouter();
                brodcaster = new Broadcaster();
                steadydetector.DetectionDuration = 200;
                
                steadydetector.Steady+=new EventHandler<SteadyEventArgs>(steadydetector_Steady);
                steadydetector.NotSteady+=new EventHandler<SteadyEventArgs>(steadydetector_NotSteady);  
              /*  pointCtrl.PrimaryPointCreate += new EventHandler<HandFocusEventArgs>(pointCtrl_PrimaryPointCreate);
                pointCtrl.PrimaryPointUpdate += new EventHandler<HandEventArgs>(pointCtrl_PrimaryPointUpdate);
                pointCtrl.PrimaryPointDestroy += new EventHandler<IdEventArgs>(pointCtrl_PrimaryPointDestroy);*/
                pointCtrl.PointCreate += new EventHandler<HandEventArgs>(pointCtrl_PointCreate);
                pointCtrl.PointUpdate += new EventHandler<HandEventArgs>(pointCtrl_PointUpdate);
                pointCtrl.PointDestroy += new EventHandler<IdEventArgs>(pointCtrl_PointDestroy);
             
        
                sessionMgr.AddListener(steadydetector);
               sessionMgr.AddListener(pointCtrl);  //make the session manager listen to the point control 

                isActive = false;                   //set lifecycle flag to false
                            //fill the handpoint coordinates with invalid values
                         //initialize the clipping matrix

                HandPointBuffer = new ArrayList();
          
            }
            catch (Exception e) { return false; }

            return true;
        }
      
        int is_push = 0;
      
        int push_counter = 0;
        int is_push_stable = 0;
        void pushDetect_Stable(object sender, VelocityEventArgs e)
       {
          
           
          //  System.Console.WriteLine("stable");
            is_push_stable = 1;
            is_push = 0;
       //     flrouter.ActiveListener = null;
            
        }
        void pushDetect_Push(object sender, VelocityAngleEventArgs e)
        {
           // System.Console.WriteLine("PushDetected");
            HandPointContact hdc = null;
            is_push = 1;
            is_push_stable = 0;
            push_counter = 0;
         /*   for (int i = 0; i < HandPointBuffer.Count; i++)
            {
                hdc = HandPointBuffer[i] as HandPointContact;

                if (hdc.Id == clickPoint.Id)
                {
                    clickPoint = hdc;
                    break;
                }
            }*/
            foreach (PointStatus pt in pointCollections)
            {
               

                if (pt.is_clicked == true)
                {
                    System.Console.WriteLine(pt.Handle.ToString());
                    pt.is_clicked = false;
                          for (int i = 0; i < HandPointBuffer.Count; i++)
                            {
                                hdc = HandPointBuffer[i] as HandPointContact;

                                if (hdc.Id == pt.Handle)
                                {
                                    clickPoint = hdc;
                                    break;
                                }
                            }
                       switch (hdc.State)
                    {
                        case Multitouch.Contracts.ContactState.Moved:  //now remove 
                            hdc.Update((int)pt.Location.X, (int)pt.Location.Y, Multitouch.Contracts.ContactState.Removed);
                            inputProvider.EnqueueContact(clickPoint, Multitouch.Contracts.ContactState.Removed);
                         //   System.Console.WriteLine("Removed..");
                            
                            pt.SetColor(Brushes.Green);
                            break;


                        case Multitouch.Contracts.ContactState.New://move
                            hdc.Update((int)pt.Location.X, (int)pt.Location.Y, Multitouch.Contracts.ContactState.Moved);
                            inputProvider.EnqueueContact(clickPoint, Multitouch.Contracts.ContactState.Moved);
                            break;
                        case Multitouch.Contracts.ContactState.Removed://create new
                            hdc.Update((int)pt.Location.X, (int)pt.Location.Y, Multitouch.Contracts.ContactState.New);
                            inputProvider.EnqueueContact(clickPoint, Multitouch.Contracts.ContactState.New);

                        //    System.Console.WriteLine("Created..");
                            pt.SetColor(Brushes.Red);
                            break;

                    }
                }
            }
           // is_steady = 0;
           // flrouter.ActiveListener = null;

        }
        void steadydetector_NotSteady(object sender, SteadyEventArgs e)
        {
           // System.Console.WriteLine("NotSteday..");
            PointStatus pt = pointCollections[e.ID];
           // if ( pt.is_non_steady==false)
            {
               // pt.nonsteady_counter = 0;
                if (pt.is_clicked == true)
                {
                    pt.SetColor(Brushes.Red);
                }
                else
                {
                    pt.SetColor(Brushes.Green); 
                }
                pt.is_non_steady = true;
                pt.is_triggered = true;
                System.Console.WriteLine("Hand is moving..:");

            }
            //flrouter.ActiveListener = pointCtrl;
            if(is_steady==1)
            {
                is_push_allow_counter = 0;
            is_push_allow = 1;
                is_steady=0;
            }
        }
        bool prev_state = true;
        HandPointContact clickPoint;
        int steady_counter = 0;
       
        void steadydetector_Steady(object sender, SteadyEventArgs e)
        {
        //   System.Console.WriteLine("Steady Detected..");
            PointStatus pt = pointCollections[e.ID];
             HandPointContact hdc = null;
             is_steady = 1;
             for (int i = 0; i < HandPointBuffer.Count; i++)
             {
                 hdc = HandPointBuffer[i] as HandPointContact;

                 if (hdc.Id == e.ID)
                 {
               
                     break;
                 }
             }
             pt.is_non_steady = false;
             pt.SetColor(Brushes.Yellow);
            int status = 0;
             Point3D hpoint = hdc.RowPoint;
             hpoint.Y = -hpoint.Y;
             Point3D pt3 = depthGen.ConvertRealWorldToProjective(hpoint);
           
             clipHandFromDepthMap(pt3);
             status = openpalm.isDetected(clipping, pt3);
             if (status == 1)
             {
                 Console.WriteLine("OpenHand");
             }
             else if(status==2)
             {
                 Console.WriteLine("Close Hand");
             }
             Multitouch.Contracts.ContactState st = hdc.State;
             if (status == 1 && (st == Multitouch.Contracts.ContactState.Moved || st == Multitouch.Contracts.ContactState.New))
             {
                 pt.SetColor(Brushes.Green);
                 pt.steady_state = 1;
                 pt.is_clicked = false;
                 
                 System.Console.WriteLine("Release...");
                 hdc.Update(pt.steady_point.X,pt.steady_point.Y, Multitouch.Contracts.ContactState.Removed);
                 inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Removed);

                 pt.steady_point = pt.Location;
             }
             else if (status == 2 && st == Multitouch.Contracts.ContactState.Removed)
             {
                 System.Console.WriteLine("Grab...");
                 pt.steady_state = 2;
                 pt.is_clicked = true;
                 pt.SetColor(Brushes.Red);
                 hdc.Update( pt.steady_point.X, pt.steady_point.Y, Multitouch.Contracts.ContactState.New);
                 inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.New);
                 pt.steady_point = pt.Location;
             }
             else
             {
                 pt.steady_point = pt.Location;
             }
      
                  
        }
        void pointCtrl_PointDestroy(object sender, IdEventArgs e)
        {

            lock (this)
            {

                for (int i = 0; i < HandPointBuffer.Count; i++)
                {
                    HandPointContact hdc = HandPointBuffer[i] as HandPointContact;
                    if (hdc.Id == e.ID)
                    {
                        hdc.destroy();
                        PointStatus pt = pointCollections[e.ID];

                      
                        pointCollections.Remove(e.ID);
                        pt.destroy();
                        inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Removed);
                        break;
                    }
                }

                Console.WriteLine("Destroyed"+e.ID.ToString()+"\n");

            }
        }
        void pointCtrl_PointUpdate(object sender, HandEventArgs e)
        {
            lock (this)
            {
                Point3D updatedhandPoint = depthGen.ConvertRealWorldToProjective(e.Hand.Position);
               updatedhandPoint = e.Hand.Position;
               updatedhandPoint.Y = -updatedhandPoint.Y;
               // Console.WriteLine("x:"+ updatedhandPoint.X +"  y:"+updatedhandPoint.Y );

                handPoint = updatedhandPoint;

                for (int i = 0; i < HandPointBuffer.Count; i++)
                {
                    HandPointContact hdc = HandPointBuffer[i] as HandPointContact;
                
                    if (hdc.Id == e.Hand.ID)
                    {
                
                        relativemotion(updatedhandPoint, hdc);
                     
                        break;
                    }
                }


            }

        }
       //needs improvement
        void relativemotion(Point3D updatedhandPoint, HandPointContact hdc)
        {
            double diffx = ( updatedhandPoint.X-hdc.RowPoint.X) *(scrWidth/((scrWidth*1)/2)) ;
            double diffy = (updatedhandPoint.Y - hdc.RowPoint.Y) * (scrHeight / ((scrHeight*1 )/ 2));
            hdc.accelerationx = diffx - hdc.velocityx;
            hdc.velocityx +=  hdc.accelerationx;
            hdc.accelerationy = diffy - hdc.velocityy;
            hdc.velocityy +=  hdc.accelerationy;
            //double scrdiffx = (((scrWidth/) * Math.Pow(diffx,1.5)) / Math.Pow(50,1.5));
         //   double scrdiffy = (((scrHeight/4) * Math.Pow(diffy,1.5)) / Math.Pow(50,1.5));
    
            PointStatus pt = pointCollections[hdc.Id];
            hdc.RowPoint = updatedhandPoint;
         /*   if (pt.is_non_steady == true)
            {
                pt.nonsteady_counter++;
                if (pt.nonsteady_counter < 11)
                {

                    pt.Location = new Point((int)(pt.Location.X + hdc.velocityx * (1 - (1 / pt.nonsteady_counter))), (int)(pt.Location.Y + hdc.velocityy * (1 - (1 / pt.nonsteady_counter))));
                }
                else
                {
                    pt.is_non_steady = false;
                    pt.nonsteady_counter = 0;
                }

            }
            else
            {
                pt.Location = new Point((int)(pt.Location.X + hdc.velocityx ), (int)(pt.Location.Y + hdc.velocityy ));
            }*/
            double xval = pt.Location.X + hdc.velocityx;
            double yval = pt.Location.Y + hdc.velocityy;
            if (xval < 0)
            {
                xval = 0;
            }
            if (yval < 0)
            {
                yval = 0;
            }
            pt.Location = new Point((int)(xval), (int)(yval));
            if (hdc.State == Multitouch.Contracts.ContactState.New || hdc.State == Multitouch.Contracts.ContactState.Moved)
            {
                hdc.Update((int)pt.Location.X, (int)pt.Location.Y, Multitouch.Contracts.ContactState.Moved);
                inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Moved);
                //System.Console.WriteLine("Moving..");
                //pt.SetColor(Brushes.Red);
            }
            /*if (pt.is_non_steady == false)
            {
                grab_gesture(hdc, pt);
            }*/
        }

        //Unused  ..
        void smoothenCursorMovement(Point3D updatedhandPoint,HandPointContact hdc)
        {
            
        /*    if (is_steady == 1)
            {

                if (steady_counter > 10)
                {
                   
                }
                is_push_allow_counter = 0;
                is_push_allow = 1;
                is_steady = 0; 
            steady_counter++;
            }*/
       
           System.Windows.Point center = hdc.OriginPoint;
            int deskHeight = scrHeight;
            int deskWidth = scrWidth;
            int h_x = 0;
            int h_y = 0;
            double power = 1.7;
            PointStatus pt = pointCollections[hdc.Id];
    
            h_x = (int)(updatedhandPoint.X - (center.X - 100));
            h_y = (int)(updatedhandPoint.Y - (center.Y - 100));

            if (h_x < 0) h_x = 0;
            if (h_y < 0) h_y = 0;

            int x = 0;
            int y = 0;
            x = h_x * deskWidth / 200;
            y = h_y * deskHeight / 200;
           if (is_steady == 1)
            {
               // Console.WriteLine("Steady...");
            }

          /* if (pt.is_non_steady == true)
           {
               double diffx = updatedhandPoint.X - hdc.Position.X;
               double diffy = updatedhandPoint.Y - hdc.Position.Y;
               if (Math.Abs(diffx) < 5 && Math.Abs(diffy) < 5)
               {
                   return;
               }
               else
               {
                   pt.is_non_steady = false;
               }
           }*/
           hdc.RowPoint = updatedhandPoint; 
           /*      if (pt.is_non_steady == true )
             {
               
                 pt.nonsteady_counter++;
            //     System.Console.WriteLine(pt.nonsteady_counter);
              //   pt.SetColor(Brushes.BlueViolet);
                 if (pt.nonsteady_counter > 10)  //stop looking for gesture
                 {
                     pt.nonsteady_counter = 0;
                     // pt.gesture_start = false;
                     pt.is_non_steady = false;
                     //System.Console.WriteLine("Grab Session Ended");
                     if (pt.is_clicked == true)
                     {
                         pt.SetColor(Brushes.Red);
                     }
                     else
                     {
                         pt.SetColor(Brushes.Green);
                     }
                 }
                if(pt.nonsteady_counter<5)
                 {
                     return;
                 }
                if (pt.nonsteady_counter > 7 && pt.nonsteady_counter < 10)
                {
                    x = (x + pt.Location.X )/ 2;
                    y = (y + pt.Location.Y )/ 2;
                }
             }*/
        /*    clipHandFromDepthMap(updatedhandPoint);
            if (is_steady == 1)
            {

            }
            int status = openpalm.isDetected(clipping, updatedhandPoint);
            if (status == 1)
            {
                //System.Console.WriteLine("open Palm");
            }
            else if (status == 2)
            {
               // System.Console.WriteLine("Close Plam");
            }
            else
            {
                System.Console.WriteLine("unknown");
            }*/

            double avgx = x;
            double avgy = y;
          


            pt.Location = new System.Drawing.Point((int)avgx, (int)avgy);
            if (hdc.State == Multitouch.Contracts.ContactState.New || hdc.State == Multitouch.Contracts.ContactState.Moved)
            {
                hdc.Update((int)avgx, (int)avgy, Multitouch.Contracts.ContactState.Moved);
                inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Moved);
                //System.Console.WriteLine("Moving..");
                //pt.SetColor(Brushes.Red);
            }
          // x = ((int)((Math.Pow(h_x, power) * (deskWidth )) / Math.Pow(200, power)));
           // y = ((int)((Math.Pow(h_y, power) * (deskHeight)) / Math.Pow(200, power)));
          /* if (h_x < change_point)
            {
                x = ((int)((Math.Pow(h_x, power) * (deskWidth*2)) / Math.Pow(200, power)));
            }
            else
            {
                x = ((int)((Math.Pow(h_x, (1/power)) * (deskWidth)) / Math.Pow(200, (1/power))));
            }
            if (h_y < change_point)
            {
                y = ((int)((Math.Pow(h_y, power) * (deskHeight)*2) / Math.Pow(200, power)));
            }
            else
            {
                y = ((int)((Math.Pow(h_y,(1/ power)) * (deskHeight)) / Math.Pow(200, (1/power))));
            }*/

// Do not delete
            /*
           double avgx = x;
          double  avgy = y;
   
          if (is_push_allow == 1 & is_push==0)
            {

              
                if (is_push_allow_counter > 3)
                {
                    is_push_allow = 0;
                   //flrouter.ActiveListener = null;
                    if (hdc.State == Multitouch.Contracts.ContactState.Removed)
                    {
                        pt.SetColor(Brushes.Green);
                    }
                    else
                    {
                        pt.SetColor(Brushes.Red);
                    }
                }
                is_push_allow_counter++;
                return;
          
            }
          if (is_push == 1 && is_push_stable==0)
            {
            
                return;
             
            }

              hdc.RowPoint = updatedhandPoint;

               
                pt.Location = new System.Drawing.Point((int)avgx, (int)avgy);
     
                //bool status = openpalm.isDetected(clipping, updatedhandPoint);
                if (hdc.State == Multitouch.Contracts.ContactState.New || hdc.State == Multitouch.Contracts.ContactState.Moved)
                {
                    hdc.Update((int)avgx, (int)avgy, Multitouch.Contracts.ContactState.Moved);
                    inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Moved);
                  //System.Console.WriteLine("Moving..");
                    //pt.SetColor(Brushes.Red);
                }
       */
          
        }

        private void clipHandFromDepthMap(Point3D HandPoint)
        {

            

                clipping = new byte[clipSize, clipSize];                    //initialize the clipping matrix

                int xStart = (int)HandPoint.X - clipSize / 2;               //starting x coordinate of the clip region
                int yStart = (int)HandPoint.Y - clipSize / 2;               //starting y coordinate of the clip region
                int xEnd = (int)HandPoint.X + clipSize / 2;                 //ending x coordinate of the clip region
                int yEnd = (int)HandPoint.Y + clipSize / 2;                 //ending y coordinate of the clip region

                int depthAtPoint = (int)HandPoint.Z;                        //save the depth of the hand point
                int deviationDepth = 50;                                    //permissible distance in mm ahead/behind the hand point

                int left = 0, right = xRes;                                 //boundary along x
                int top = 0, bottom = yRes;                                 //boundary along y

                /*
                 *the loop below scans the depth map from the starting (x,y) coords to the ending (x,y) coords and thresholds each
                 *point within this window based on its depth. If the depth of this point is within a permissible distance of the hand
                 *point, then it is saved, else it is discarded.
                 
                 *the boundary conditions are observed and only points within the boundary of the 640 x 480 are considered. Sometimes, if the hand
                 * point is near the edges, the clipping region will extend outside the boundaries. In this case only the points which lie in the clipping
                 * area and are within the boundary are taken for thresholding.
                 */

                for (int j = yStart; j < yEnd; j++)
                {
                    for (int i = xStart; i < xEnd; i++)
                    {
                        byte valueToSet = 0;

                        //boundary condition check made here. The point being considered must be within the 640x480 boundary
                        if ((i >= left && i < right) && (j >= top && j < bottom))
                        {
                            int depth = depthMeta[i, j];    //read the depth at this point

                            /* 
                             * this point (i,j) is within the boundary, so lets check if its within the permissible depth. 
                             * if yes, save it as a bright spot (MAX_COLOR)
                             */
                            if ((depth < depthAtPoint + deviationDepth) && (depth > depthAtPoint - deviationDepth))
                                valueToSet = (byte)255;
                        }

                        //the if clause above will help us decide the value of the point (either 0 or 255), now just fill the 
                        //clip matrix with this value
                        clipping[i - xStart, j - yStart] = valueToSet;
                    }
                }
            
         
        }
        void pointCtrl_PointCreate(object sender, HandEventArgs e)
        {
           

          //  Console.WriteLine("Created..\n");
         
            handPoint = depthGen.ConvertRealWorldToProjective(e.Hand.Position);
            handPoint = e.Hand.Position;
            handPoint.Y = -handPoint.Y;
         //   handPoint = depthGen.ConvertProjectiveToRealWorld(e.Hand.Position);

            HandPointContact hdc = new HandPointContact(e.Hand.ID, (int)((handPoint.X * scrWidth) / xRes), (int)(((handPoint.Y) * scrHeight) / yRes), Multitouch.Contracts.ContactState.Removed);
            hdc.RowPoint = handPoint;
            hdc.prev_RowPoint = handPoint;
            hdc.OriginPoint =new System.Windows.Point(hdc.RowPoint.X,hdc.RowPoint.Y);
            hdc.timestamp = e.Hand.Time;
           // HandPointContact hdc = new HandPointContact(e.Hand.ID, (int)(handPoint.X ), (int)((handPoint.Y) ), Multitouch.Contracts.ContactState.New);

            HandPointBuffer.Add(hdc);
            for (int i = 0; i < 5; i++)
            {
                pbuffer[i] = new Point((int)hdc.Position.X,(int)hdc.Position.Y);
                point_counter = 1;
            }
         
            inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Removed);
            pointCollections.Add(new PointStatus(hdc));
            PointStatus pt = pointCollections[hdc.Id];
          
            Console.WriteLine("Created" + e.Hand.ID.ToString() + " Hello  Time:\n" + e.Hand.Time.ToString());
        }
       /* void pointCtrl_PrimaryPointDestroy(object sender, IdEventArgs e)
        {
            
            lock (this)
            {
            
                for (int i = 0; i < HandPointBuffer.Count; i++)
                {
                    HandPointContact hdc = HandPointBuffer[i] as HandPointContact;
                    if (hdc.Id == e.ID)
                    {
                        hdc.destroy();
                       
                        inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Removed); 
                        break;
                    }
                }
             
                Console.WriteLine("Destroyed"+e.ID.ToString()+"\n");
               
            }
        }
        void pointCtrl_PrimaryPointUpdate(object sender, HandEventArgs e)
        {
            lock (this)
            {
                Point3D updatedhandPoint = depthGen.ConvertRealWorldToProjective(e.Hand.Position);
              
                handPoint = updatedhandPoint;
              
                for (int i = 0; i < HandPointBuffer.Count; i++)
                {
                    HandPointContact hdc = HandPointBuffer[i] as HandPointContact;
                    if (hdc.Id == e.Hand.ID)
                    {
                        hdc.Update((int)e.Hand.Position.X, (int)e.Hand.Position.Y);
                        inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.Moved);
                       // Console.WriteLine("Update " + e.Hand.ID.ToString() + "\n");
                        break;
                    }
                }

               
            }

        }
        void pointCtrl_PrimaryPointCreate(object sender, HandFocusEventArgs e)
        {


           // Console.WriteLine("Created..\n");
            
            handPoint = depthGen.ConvertRealWorldToProjective(e.Hand.Position);

            HandPointContact hdc = new HandPointContact(e.Hand.ID, (int)handPoint.X, (int)handPoint.Y, Multitouch.Contracts.ContactState.New);
         
          HandPointBuffer.Add(hdc);
         
            
          inputProvider.EnqueueContact(hdc, Multitouch.Contracts.ContactState.New);
       
          Console.WriteLine("Created"+e.Hand.ID.ToString()+"\n");
        }*/
        public bool isRunning()
        {
            return isActive;
        }
        public void start()
        {
            isActive = true;
            this.updateThread = new Thread(this.updateRoutine);
            this.updateThread.Start();
        }
        public void stop()
        {
            this.isActive = false;
        }
        private unsafe void updateRoutine()
        {
          
            while (isActive)
            {
                //try
                {
                    cxt.WaitAndUpdateAll();                                         //update the depth node 
                    sessionMgr.Update(cxt);                                                 //update the session manager
                                        //get the meta data from the depth node
                   // Console.WriteLine("Updateting..\n");
                    depthMeta = depthGen.GetMetaData(); 
                    inputProvider.RaiseNewFrame(frameCounter);                                               //clip that meta data if hand point is valid. The clip is saved in clipping matrix

                }
                frameCounter++;

                //catch (Exception e) { }
            }
        }

    }
}
