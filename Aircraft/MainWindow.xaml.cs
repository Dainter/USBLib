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
using System.Threading;
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
        
        #region SET YOUR USB Vendor and Product ID!
        public static UsbDevice MyUsbDevice;
        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x046d, 0xc05a);
        public static UsbEndpointReader reader;
        public static Thread USBthread;
        #endregion  

        public MainWindow()
        {
            StatusUpdateTimer_Init();
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


        private void USBDeviceInit()
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
            reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
        }

        private void USBDeviceRelease()
        {
            if (MyUsbDevice == null)
            {
                return;
            }
            if (MyUsbDevice.IsOpen == false)
            {
                return;
            }
            if (!ReferenceEquals(reader, null))
            {
                // Release interface #0.  
               reader.Dispose();
            }
            IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
            if (!ReferenceEquals(wholeUsbDevice, null))
            {
                // Release interface #0.  
                wholeUsbDevice.ReleaseInterface(0);
            }
            MyUsbDevice.Close();
            MyUsbDevice = null;
            // Free usb resources  
            UsbDevice.Exit();
        }

        private void USBMouseCapture()
        {
            ErrorCode ec = ErrorCode.None;
            byte[] readBuffer = new byte[4];
            int bytesRead = 0;
            int intXoffset;
            int intYoffset;
            int intXpos;
            int intYpos;

            while (true)
            {
                Thread.Sleep(5);
                //this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new UpdateDataDelegate(UpdateData), new object[] { bytesRead, bytesRead });
                // If the device hasn't sent data in the last 5 seconds,  
                // a timeout error (ec = IoTimedOut) will occur.   
                ec = reader.Read(readBuffer, 5000, out bytesRead);
                for (int index = 0; index < bytesRead; index++)
                {
                    bool isPositive;
                    byte bytDisplay;
                    string strDisplay = "";
                    if (index == 1)
                    {
                        bytDisplay = CompleToOrig(readBuffer[index], out isPositive);
                        if (isPositive == true)
                        {
                            strDisplay = "-";
                        }
                        strDisplay += bytDisplay.ToString();
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                        (ThreadStart)delegate()
                            {
                                intXpos = (int)Y_axis.Value;
                                intXoffset = Convert.ToInt32(strDisplay);
                                Y_axis.Value = CalAngle(intXpos, intXoffset);
                                Z_axis.Value = Y_axis.Value;
                                this.Resources["XPosition"] = strDisplay;

                            }
                        );
                        continue;
                    }
                    else if (index == 2)
                    {
                        bytDisplay = CompleToOrig(readBuffer[index], out isPositive);
                        if (isPositive == false)
                        {
                            strDisplay = "-";
                        }
                        strDisplay += bytDisplay.ToString();
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                        (ThreadStart)delegate()
                            {
                                intYpos = (int)X_axis.Value;
                                intYoffset = Convert.ToInt32(strDisplay);
                                X_axis.Value = CalAngle(intYpos, intYoffset);
                                this.Resources["YPosition"] = strDisplay;
                            }
                        );
                        continue;
                    }
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

        int CalAngle(int iPos, int iOffset)
        {
            const int MaxAngle = 45;
            int newPos;
            if (iOffset == 0)
            {
                return iPos;
            }
            newPos = iPos + iOffset;
            if (Math.Abs(newPos) < MaxAngle)
            {
                return newPos;  
            }
            if (newPos > 0)
            {
                return MaxAngle;
            }
            return -MaxAngle;
        }

        private void CaptureButton_Checked(object sender, RoutedEventArgs e)
        {
            USBthread = new Thread(USBMouseCapture);
            USBDeviceInit();
            USBthread.Start();
        }

        private void CaptureButton_Unchecked(object sender, RoutedEventArgs e)
        {
            USBthread.Abort();
            USBDeviceRelease();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OnClosing();
        }

        private void OnClosing()
        {
            if (USBthread.ThreadState != ThreadState.Unstarted)
            {
                USBthread.Abort();
            }
            if (MyUsbDevice != null)
            {
                USBDeviceRelease();
            }
        }

    }
}
