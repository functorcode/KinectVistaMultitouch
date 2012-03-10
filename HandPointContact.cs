using System;
using System.Windows;
using Multitouch.Contracts;
using OpenNI;
namespace NITEProvider
{
    class HandPointContact : Contact
    {
       public Point OriginPoint;
       public double velocityx=0;
       public double velocityy = 0;
       public double accelerationx = 0;

       public double accelerationy=0;
       public Point3D RowPoint;
       public Point3D prev_RowPoint;
       public double timestamp;
        public HandPointContact(HandPointContact hc, ContactState state):
            base(hc.Id, state,hc.Position, 20, 20)
        {
            velocityx = 0;
            velocityy = 0;
          //  CenterPoint = new Point(hc.Position.X, hc.Position.Y);
           
        }
        public HandPointContact(int id, int x, int y, ContactState state)
                : base(id, state,new Point(x,y), 20, 20)
            {
              //  CenterPoint = new Point(x,y);
                velocityy = 0;
                velocityx = 0;
                Orientation = 0;
            }
        public void Update(int x,int y,ContactState state)
        {
            Position = new Point(x, y);
            State = state;
        
            Orientation = 0;

        }
        public void destroy()
        {
            State = ContactState.Removed;
        }
    }
}
