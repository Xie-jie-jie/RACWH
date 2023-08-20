#pragma once
#ifndef _SIMPLEUSB_H_
#define _SIMPLEUSB_H_

#include "utils.h"
#include "usb_device_opt.h"

//To initialize libusb, return 0 if success
extern "C" __declspec(dllexport)  int __stdcall USB_Init();

//To exit libusb
extern "C" __declspec(dllexport)  void __stdcall USB_Exit();

//To get device list, return 0 if success
extern "C" __declspec(dllexport)  int __stdcall USB_Get_Device_List();

//To scan USB device, similar to USB_Get_Device_List()
extern "C" __declspec(dllexport)  void __stdcall USB_Scan_Device();

//To open USB device, return pMsd if success, NULL if failure
extern "C" __declspec(dllexport)  void* __stdcall USB_Open(uint16_t VID, uint16_t PID);

//To write USB device, return actual write size if success, <0 if failure
extern "C" __declspec(dllexport)  int __stdcall USB_Write(void* pMsd, uint8_t* buff, int len, uint32_t ms);

//To read USB device, return actual read size if success, <0 if failure
extern "C" __declspec(dllexport)  int __stdcall USB_Read(void* pMsd, uint8_t * buff, int len, uint32_t ms);

//To close USB device 
extern "C" __declspec(dllexport)  void __stdcall USB_Close(void* pMsd);

#endif