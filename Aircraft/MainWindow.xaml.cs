using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using System.Windows.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.Info;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.WinUsb;  

namespace Aircraft
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        
        
        public static UsbDevice MyUsbDevice;

        #region SET YOUR USB Vendor and Product ID!

        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x046d, 0xc05a);

        #endregion  
        public MainWindow()
        {
            StatusUpdateTimer_Init();
            DataUpadteTimer_Init();
        }

        #region StatusTimer
        DispatcherTimer StatusUpadteTimer;

        private void StatusUpdateTimer_Init()
        {
            StatusUpadteTimer = new DispatcherTimer();
            StatusUpadteTimer.Interval = new TimeSpan(0, 0, 3);
            StatusUpadteTimer.Tick += new EventHandler(StatusUpdateTimer_Tick);
            StatusUpadteTimer.IsEnabled = false;
        }

        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            StatusLabel.Content = "Ready";
            StatusUpadteTimer.IsEnabled = false;
        }

        public void ShowStatus(string sStatus)
        {
            StatusLabel.Content = sStatus;
            StatusUpadteTimer.Start();
        }
        #endregion

        #region DataUpdateTimer
        DispatcherTimer DataUpadteTimer;
        private string strXoffset;
        private string strYoffset;

        private void DataUpadteTimer_Init()
        {
            DataUpadteTimer = new DispatcherTimer();
            DataUpadteTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            DataUpadteTimer.Tick += new EventHandler(DataUpadteTimer_Tick);
            DataUpadteTimer.IsEnabled = false;
            strXoffset = "0";
            strYoffset = "0";
        }

        private void DataUpadteTimer_Tick(object sender, EventArgs e)
        {
            XOffsetLabel.Content = strXoffset;
            YOffsetLabel.Content = strYoffset;
        }
        #endregion
        

        private void MouseCapture()
        {
            ErrorCode ec = ErrorCode.None;

            try
            {
                // Find and open the usb device.  
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
                // If the device is open and ready  
                if (MyUsbDevice == null)
                {
                    throw new Exception("Device Not Found.");
                }
                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used,   
                    // the desired configuration and interface must be selected.  
                    // Select config #1  
                    wholeUsbDevice.SetConfiguration(1);
                    // Claim interface #0.  
                    wholeUsbDevice.ClaimInterface(0);
                }
                // open read endpoint 1.  
                UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                byte[] readBuffer = new byte[4];
                while (ec == ErrorCode.None)
                {
                    int bytesRead;
                    // If the device hasn't sent data in the last 5 seconds,  
                    // a timeout error (ec = IoTimedOut) will occur.   
                    ec = reader.Read(readBuffer, 5000, out bytesRead);

                    if (bytesRead == 0)
                    {
                        throw new Exception(string.Format("{0}:No more bytes!", ec));
                    }
                    // Write that output to the console.  
                    for (int index = 0; index < bytesRead; index++)
                    {
                        bool isPositive;
                        byte bytDisplay;
                        string strDisplay = "";
                        if (index == 1 )
                        {
                            bytDisplay = CompleToOrig(readBuffer[index], out isPositive);
                            if (isPositive == false)
                            {
                                strDisplay = "-";
                            }
                            strDisplay += bytDisplay.ToString();
                            strXoffset = strDisplay;
                            continue;
                        }
                        else if(index == 2)
                        {
                            bytDisplay = CompleToOrig(readBuffer[index], out isPositive);
                            if (isPositive == false)
                            {
                                strDisplay = "-";
                            }
                            strDisplay += bytDisplay.ToString();
                            strYoffset = strDisplay;
                            continue;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message.ToString());
                return;
            }
            finally
            {
                if (MyUsbDevice != null)
                {
                    if (MyUsbDevice.IsOpen)
                    {
                        IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                        if (!ReferenceEquals(wholeUsbDevice, null))
                        {
                            // Release interface #0.  
                            wholeUsbDevice.ReleaseInterface(0);
                        }
                        MyUsbDevice.Close();
                    }
                    MyUsbDevice = null;
                    // Free usb resources  
                    UsbDevice.Exit();
                }
                
            }  
        }

        byte CompleToOrig(byte byt, out bool isPositive)
        {
            int iComple = (int)byt;
            byte bResult;

            if (byt < 0xC0)
            {
                isPositive = true;
                return byt;
            }
            isPositive = false;
            bResult = (byte)~byt;
            return (byte)(bResult + 1);
        }

        private void CaptureButton_Checked(object sender, RoutedEventArgs e)
        {
            DataUpadteTimer.IsEnabled = true;
            MouseCapture();
            DataUpadteTimer.IsEnabled = false;
            CaptureButton.IsChecked = false;
        }

    }
}
