using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using MultipleMice.Native;
namespace NITEProvider
{
    class PointStatus
    {
        readonly HandPointContact device;
		readonly  debugPoints debugpoints;
		Point location;
        public Point temp_location;
        Point[] buffer;
        Point[] buffer2;
        Thread t;
        int buffer_counter = 0;
       public bool is_clicked = false;
       public bool gesture_start = false;
       public bool is_non_steady = false;
       public int nonsteady_counter = 0;
       public int steady_state = 1;
       public bool is_triggered = false;
       private int frame_counter = 0;
       public Point steady_point;
        public PointStatus(HandPointContact device)
		{
			this.device = device;
            buffer = new Point[2]; //unused
            buffer2 = new Point[6]; //unused
            buffer[0] = new Point((int)device.Position.X, (int)device.Position.Y);//unused
            buffer[1] = new Point((int)device.Position.X, (int)device.Position.Y);//unused
            buffer_counter = 1;//unused
            debugpoints = new debugPoints();
            debugpoints.Name = "DebugCursor";
			Win32.POINT position = Win32.GetCursorPosition();
			Location = new Point(position.x, position.y);
           
			t = new Thread(ThreadWorker);
			t.Name = "Cursor for device: " + device.Id;
			t.SetApartmentState(ApartmentState.STA);
			t.IsBackground = true;
			t.Start();
		}
        private void add_to_buffer(Point p) //unused
        {
            SynchronizationContext current = SynchronizationContext.Current;
            current.Send(sync_setcolor, p);
        }
        private void sync_buffer_point(Object p) //unused
        {
            buffer[buffer_counter] = (Point)p;
            buffer_counter++;
            if (buffer_counter > 1)
            {
                buffer_counter = 0;
            }

        }
        private void Update_Location(int newx, int newy) //unused
        {
            SynchronizationContext current = SynchronizationContext.Current;
            current.Send(sync_setcolor,new Point(newx,newy));
        }
        private void sync_update_location(Object ob) //unused
        {
            Point p = (Point)ob;
            int avgx = p.X;
            int avgy = p.Y;
            for (int i = 0; i < 2; i++)
            {
                avgx += buffer[i].X;
                avgy += buffer[i].Y;

            }
            avgx = avgx / 3;
            avgy = avgy / 3;

            buffer_counter++;
            if (buffer_counter > 1)
            {
                buffer_counter = 0;
            }
            buffer[buffer_counter] = new Point(avgx, avgy);
            location = buffer[buffer_counter];
        }

        void ThreadWorker()
		{
            debugpoints.Show(Location);
            Application.Run(debugpoints);
		}
        public void destroy()
        {
            debugpoints.Dispose();
            t.Abort();
        }
        public void SetColor(Brush br)
        {
            SynchronizationContext current = SynchronizationContext.Current;
            current.Send(sync_setcolor, br);
         
            
        }
        public void sync_setcolor(Object br)
        {
            debugpoints.set_actionimage((Brush)br);
           
        }
        public Point Location
        {
            get { return location; }
            set
            {
                if (location != value)
                {
                    location = value;
                    UpdateLocation();
                }
            }
        }
	/*	public Point Location
		{
			get { return location; }
		set
			{
                if (location != value)
                {
                    int avgx = value.X;
                   
                    int avgy = value.Y;
                    if (is_non_steady == true)
                    {
                        if (is_triggered == true)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                buffer2[i]=new Point(value.X,value.Y);
                                

                            }
                            is_triggered = false;
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            avgx += buffer2[i].X;
                            avgy += buffer2[i].Y;

                        }
                        avgx = avgx / 7;
                        avgy = avgy / 7;
                        nonsteady_counter++;
                        if (nonsteady_counter > 5)
                        {
                            nonsteady_counter = 0;
                          
                        }
                        frame_counter++;
                        if (frame_counter > 15)
                        {
                            frame_counter = 0;
                            is_non_steady = false;
                            buffer[1] = new Point(avgx, avgy);
                            buffer[0] = new Point(avgx, avgy);
                        }
                        buffer2[nonsteady_counter] = new Point(avgx, avgy);
                        location = buffer2[nonsteady_counter];
                    }
                    else
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            avgx += buffer[i].X;
                            avgy += buffer[i].Y;

                        }
                        avgx = avgx / 3;
                        avgy = avgy / 3;

                        buffer_counter++;
                        if (buffer_counter > 1)
                        {
                            buffer_counter = 0;
                        }
                        buffer[buffer_counter] = new Point(avgx, avgy);
                        location = buffer[buffer_counter];
                    }
                   
					UpdateLocation();
				 }
			}
		}*/

		public int Handle
		{
			get { return device.Id; }
		}

		void UpdateLocation()
		{
			SynchronizationContext current = SynchronizationContext.Current;
			current.Send(SyncUpdateLocation, null);
		}

		void SyncUpdateLocation(object state)
		{
            debugpoints.Location = new Point(Location.X - debugpoints.Width / 2, Location.Y - debugpoints.Height / 2);
		}

		
    }
}
