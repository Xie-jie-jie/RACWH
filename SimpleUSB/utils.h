#pragma once
#include <iostream>
#include "libusb.h"
#pragma comment(lib, "libusb-1.0.lib")
#include "usb_device_opt.h"

/// <summary>
/// 获取设备列表
/// </summary>
/// <returns>0 成功 -1 失败</returns>
int Get_Device_List();

/// <summary>
/// 详细扫描USB设备
/// </summary>
void USB_Scan_Print();

/// <summary>
/// 设置发送字符串
/// </summary>
/// <param name="s">要发送的字符串</param>
/// <param name="buff_Tx">Tx缓冲区</param>
/// <param name="size">缓冲区大小</param>
void Set_Tx_String(std::string s, uint8_t* buff_Tx, int size);