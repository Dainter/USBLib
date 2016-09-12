using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.Info;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.WinUsb;  

namespace USBLib
{
    class Program
    {
        public static UsbDevice MyUsbDevice;

        #region SET YOUR USB Vendor and Product ID!

        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x046d, 0xc05a);

        #endregion  

        static void Main(string[] args)
        {
            ErrorCode ec = ErrorCode.None;  
  
            try  
            {  
                // Find and open the usb device.  
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
                
                // If the device is open and ready  
                if (MyUsbDevice == null) throw new Exception("Device Not Found.");

                Program.DeviceInfo();
                // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)  
                // it exposes an IUsbDevice interface. If not (WinUSB) the   
                // 'wholeUsbDevice' variable will be null indicating this is   
                // an interface of a device; it does not require or support   
                // configuration and interface selection.  
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
  
  
                byte[] readBuffer = new byte[1024];  
                while (ec == ErrorCode.None)  
                {  
                    int bytesRead;  
  
                    // If the device hasn't sent data in the last 5 seconds,  
                    // a timeout error (ec = IoTimedOut) will occur.   
                    ec = reader.Read(readBuffer, 5000, out bytesRead);  
  
                    if (bytesRead == 0) throw new Exception(string.Format("{0}:No more bytes!", ec));  
                    Console.WriteLine("{0} bytes read", bytesRead);  
  
                    // Write that output to the console.  
                    for (int index = 0; index < bytesRead; index++)
                    {
                        Console.Write("0x{0}\t", readBuffer[index].ToString("x"));
                        if ((index+1) % 8 == 0)
                        {
                            Console.WriteLine("");  
                        }
                    }
                    
                }  
  
                Console.WriteLine("\r\nDone!\r\n");  
            }  
            catch (Exception ex)  
            {  
                Console.WriteLine();  
                Console.WriteLine((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);  
            }  
            finally  
            {  
                if (MyUsbDevice != null)  
                {  
                    if (MyUsbDevice.IsOpen)  
                    {  
                        // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)  
                        // it exposes an IUsbDevice interface. If not (WinUSB) the   
                        // 'wholeUsbDevice' variable will be null indicating this is   
                        // an interface of a device; it does not require or support   
                        // configuration and interface selection.  
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
  
                // Wait for user input..  
                Console.ReadKey();  
            }  
        }

        static void DeviceInfo()
        {
            Console.WriteLine("Vendor ID: 0x{0}", MyUsbDevice.Info.Descriptor.VendorID.ToString("x"));
            Console.WriteLine("Product ID: 0x{0}", MyUsbDevice.Info.Descriptor.ProductID.ToString("x"));
            Console.WriteLine("Manufacturer String: {0}", MyUsbDevice.Info.ManufacturerString);
            Console.WriteLine("Product String: {0}", MyUsbDevice.Info.ProductString);

            foreach (KeyValuePair<string, object> kvp in MyUsbDevice.UsbRegistryInfo.DeviceProperties)
            {
                Console.WriteLine("{0}:", kvp.Key);
                Type type = kvp.Value.GetType();
                switch (type.Name.ToLower())
                {
                    case "string[]":
                        string[] strValues = kvp.Value as string[];
                        foreach (string strSeg in strValues)
                        {
                            Console.WriteLine("  {0}", strSeg);
                        }
                        break;
                    case "int":
                        Console.WriteLine("\t0x{0}", ((int)kvp.Value).ToString("x"));
                        break;
                    default:
                        Console.WriteLine("  {0}", kvp.Value.ToString());
                        break;
                }
            }

        }

    }
}