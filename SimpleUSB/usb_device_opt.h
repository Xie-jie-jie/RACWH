#pragma once

#include <stdio.h>
#include "libusb.h"
#pragma comment(lib, "libusb-1.0.lib")
#include <stdint.h>

typedef struct USB_MSD
{
    libusb_device* msd_dev;
    libusb_device_handle* msd_handle;
    uint8_t endpoint_in;
    uint8_t endpoint_out;
} USB_MSD_ST;

extern uint8_t USB_Dev_Scan_A_Print(int16_t dev_num, libusb_device** devs);
extern uint8_t USB_MSD_Open(uint16_t VID, uint16_t PID, USB_MSD_ST* msd);
extern uint8_t USB_MSD_Bulk_Write(USB_MSD_ST* msd, uint8_t* buffer, int len, int* size, uint32_t ms);
extern uint8_t USB_MSD_Bulk_Read(USB_MSD_ST* msd, uint8_t* buffer, int len, int* size, uint32_t ms);
extern uint8_t USB_MSD_Close(USB_MSD_ST* msd);
