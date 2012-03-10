using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Timers;
using Multitouch.Contracts;
namespace NITEProvider
{
    [AddIn("NITE", Publisher = "Juned Munshi", Description = "Provides input from NITE.", Version = VERSION)]
    [Export(typeof(IProvider))]
    public class InputProvider : IProvider
    {
       
        internal const string VERSION = "1.0.0.0";
        readonly Queue<Contact> contactsQueue;
        SensorHandler sensorObject;
        public event EventHandler<NewFrameEventArgs> NewFrame;
       
        public InputProvider()
        {
          
            contactsQueue = new Queue<Contact>();
            
           // monitorSize = SystemInformation.PrimaryMonitorSize;
            SendEmptyFrames = false;
        }
     
        public bool SendImageType(ImageType imageType, bool isEnable)
        {
            return false;
        }

        public bool SendEmptyFrames { get; set; }

        public void Start()
        {
           
            bool status = false;
            sensorObject = new SensorHandler(this);
            string xmlPath =  System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config\\openni.xml";
            status = sensorObject.initializeSensor(xmlPath);
            if (status)                                             //initialization was successful
            {
                System.Console.WriteLine("Nite Started...");
                sensorObject.start();   
                IsRunning = true;
            }
            else
            {
                IsRunning = false;
            }
        }

        public void Stop()
        {
            if (sensorObject != null)
            {
                sensorObject.stop();     
                IsRunning = false;
            }
        }

       

        public bool IsRunning { get; internal set; }

     

        internal void EnqueueContact(HandPointContact cursor, ContactState state)
        {
            lock (contactsQueue)
            {
                contactsQueue.Enqueue(new HandPointContact(cursor, state));
            }
        }
        public UIElement GetConfiguration()
        {
            return new NiteConfiguration();
        }

        public bool HasConfiguration
        {
            get { return false; }
        }

        internal void RaiseNewFrame(long timestamp)
        {
            lock (contactsQueue)
            {
                if (SendEmptyFrames || contactsQueue.Count > 0)
                {
                    EventHandler<NewFrameEventArgs> eventHandler = NewFrame;
                    if (eventHandler != null)
                        eventHandler(this, new NewFrameEventArgs(timestamp, contactsQueue, null));
                    contactsQueue.Clear();
                }
            }
        }

    }
}
