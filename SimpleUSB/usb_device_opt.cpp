
#include <stdio.h>
#include <stdint.h>
#include "usb_device_opt.h"

uint8_t USB_Dev_Scan_A_Print(int16_t dev_num, libusb_device** devs)
{
    struct libusb_device_descriptor usb_dev_desc;
    struct libusb_config_descriptor* usb_cfg_desc;
    uint8_t err = 0;


    printf("Dev_Num:%d", dev_num);
    for (uint16_t i = 0; i < dev_num; i++)
    {
        err = libusb_get_device_descriptor(devs[i], &usb_dev_desc);
        if (err > 0)
        {
            printf("libusb_get_device_descriptor()  err with %d", err);
            goto getdevdesc_err;
        }

        printf("|--[Vid:0x%04x, Pid:0x%04x]-[Class:0x%02x, SubClass:0x%02x]-[bus:%d, device:%d, port:%d]-[cfg_desc_num:%d]\n",
            usb_dev_desc.idVendor, usb_dev_desc.idProduct, usb_dev_desc.bDeviceClass, usb_dev_desc.bDeviceSubClass,
            libusb_get_bus_number(devs[i]), libusb_get_device_address(devs[i]), libusb_get_port_number(devs[i]), usb_dev_desc.bNumConfigurations);
        //printf()
        for (uint8_t j = 0; j < usb_dev_desc.bNumConfigurations; j++)
        {
            err = libusb_get_config_descriptor(devs[i], j, &usb_cfg_desc);
            if (err > 0)
            {
                printf("libusb_get_config_descriptor(cfg_index:%d)  err with %d", j, err);
                goto getcfgdesc_err;
            }
            printf("|  |--cfg_desc:%02d-[cfg_value:0x%01x]-[infc_desc_num:%02d]\n",
                j, usb_cfg_desc->bConfigurationValue, usb_cfg_desc->bNumInterfaces);
            for (uint8_t l = 0; l < usb_cfg_desc->bNumInterfaces; l++)
                for (uint8_t n = 0; n < usb_cfg_desc->interface[l].num_altsetting; n++)
                {
                    printf("|  |  |--intfc_desc: %02d:%02d-[Class:0x%02x, SubClass:0x%02x]-[ep_desc_num:%02d]\n",
                        l, n, usb_cfg_desc->interface[l].altsetting[n].bInterfaceClass, usb_cfg_desc->interface[l].altsetting[n].bInterfaceSubClass,
                        usb_cfg_desc->interface[l].altsetting[n].bNumEndpoints);
                    for (uint8_t m = 0; m < usb_cfg_desc->interface[l].altsetting[n].bNumEndpoints; m++)
                    {
                        printf("|  |  |  |--ep_desc:%02d-[Add:0x%02x]-[Attr:0x%02x]-[MaxPkgLen:%02d]\n",
                            m, usb_cfg_desc->interface[l].altsetting[n].endpoint[m].bEndpointAddress,
                            usb_cfg_desc->interface[l].altsetting[n].endpoint[m].bmAttributes,
                            usb_cfg_desc->interface[l].altsetting[n].endpoint[m].wMaxPacketSize);
                    }
                }
        }
    }
    return 0;
getdevdesc_err:
    return 0xff;
getcfgdesc_err:
    return 0xff;
}

uint8_t USB_MSD_Open(uint16_t VID, uint16_t PID, USB_MSD_ST* msd)
{
    struct libusb_device_descriptor usb_dev_desc;
    struct libusb_config_descriptor* usb_cfg_desc = nullptr;
    uint8_t intfc_index = 0;
    uint8_t err = 0;

    msd->msd_handle = libusb_open_device_with_vid_pid(NULL, VID, PID);
    if (msd->msd_handle == NULL)
    {
        printf("[0x%04x:0x%04x] MSD Open failed!", VID, PID);
        goto opendev_err;
    }
    msd->msd_dev = libusb_get_device(msd->msd_handle);
    if (msd->msd_dev == NULL)
    {
        printf("[0x%04x:0x%04x] get dev failed!", VID, PID);
        goto getdev_err;
    }
    err = libusb_get_device_descriptor(msd->msd_dev, &usb_dev_desc);
    if (err > 0)
    {
        printf("[0x%04x:0x%04x] get dev_desc failed err with %d", VID, PID, err);
        goto getdevdesc_err;
    }
    err = libusb_get_config_descriptor(msd->msd_dev, 0, &usb_cfg_desc);
    if (err > 0)
    {
        printf("[0x%04x:0x%04x] get cfg_desc failed err with %d", VID, PID, err);
        goto getcfgdesc_err;
    }
    for (uint8_t m = 0; m < usb_cfg_desc->bNumInterfaces; m++)
    {
        for (uint8_t n = 0; n < usb_cfg_desc->interface[m].num_altsetting; n++)
        {
            if (usb_cfg_desc->interface[m].altsetting[n].bInterfaceClass == 0x0a && usb_cfg_desc->interface[m].altsetting[n].bInterfaceSubClass == 0x00)
            {
                for (uint8_t i = 0; i < usb_cfg_desc->interface[m].altsetting[n].bNumEndpoints; i++)
                {
                    if ((usb_cfg_desc->interface[m].altsetting[n].endpoint[i].bmAttributes & LIBUSB_TRANSFER_TYPE_MASK) == LIBUSB_TRANSFER_TYPE_BULK)
                    {
                        if ((usb_cfg_desc->interface[m].altsetting[n].endpoint[i].bEndpointAddress & 0x80) == LIBUSB_ENDPOINT_IN)
                        {
                            msd->endpoint_in = usb_cfg_desc->interface[m].altsetting[n].endpoint[i].bEndpointAddress;
                        }
                        if ((usb_cfg_desc->interface[m].altsetting[n].endpoint[i].bEndpointAddress & 0x80) == LIBUSB_ENDPOINT_OUT)
                        {
                            msd->endpoint_out = usb_cfg_desc->interface[m].altsetting[n].endpoint[i].bEndpointAddress;
                        }
                    }
                }
                if (msd->endpoint_in != 0x00 && msd->endpoint_out != 0x00)
                {
                    intfc_index = m;
                }
                else
                {
                    msd->endpoint_in = 0x00;
                    msd->endpoint_out = 0x00;
                }
            }
        }
    }
    if (msd->endpoint_in == 0x00 || msd->endpoint_out == 0x00)
    {
        printf("[0x%04x:0x%04x] get ep_addr failed!", VID, PID);
        goto getepaddr_err;
    }

    err = libusb_claim_interface(msd->msd_handle, 1);
    err = libusb_set_interface_alt_setting(msd->msd_handle, 1, 0);
    err = libusb_claim_interface(msd->msd_handle, 0);
    err = libusb_set_interface_alt_setting(msd->msd_handle, 0, 0);
    if (err > 0)
    {
        printf("[0x%04x:0x%04x] claim intfc failed err with %d", VID, PID, err);
        goto claimintfc_err;
    }

    err = libusb_clear_halt(msd->msd_handle, msd->endpoint_out);
    if (err > 0)
    {
        printf("[0x%04x:0x%04x] ep_out:%x clear halt failed err with %d", VID, PID, msd->endpoint_out, (int8_t)err);
        goto epclrhalt_err;
    }

    err = libusb_clear_halt(msd->msd_handle, msd->endpoint_in);
    if (err > 0)
    {
        printf("[0x%04x:0x%04x] ep_in:%x clear halt failed err with %d", VID, PID, msd->endpoint_in, (int8_t)err);
        goto epclrhalt_err;
    }

    libusb_free_config_descriptor(usb_cfg_desc);
    libusb_reset_device(msd->msd_handle);

    return 0;

claimintfc_err:
    libusb_release_interface(msd->msd_handle, 1);
    libusb_release_interface(msd->msd_handle, 0);

detachkernel_err:

epclrhalt_err:

getepaddr_err:

getcfgdesc_err:

getdevdesc_err:

getdev_err:

opendev_err:
    libusb_free_config_descriptor(usb_cfg_desc);
    libusb_close(msd->msd_handle);
    return 0xff;
}

uint8_t USB_MSD_Close(USB_MSD_ST* msd)
{
    if (msd->msd_handle != NULL)
    {
        libusb_release_interface(msd->msd_handle, 0);
        libusb_close(msd->msd_handle);
    }
    return 0;
}

uint8_t USB_MSD_Bulk_Write(USB_MSD_ST* msd, uint8_t* buffer, int len, int* size, uint32_t ms)
{
    int err = 0;

    err = libusb_bulk_transfer(msd->msd_handle, msd->endpoint_out, buffer, len, size, ms);
    if (err < 0)
    {
        return err;
    }

    return 0;
}

uint8_t USB_MSD_Bulk_Read(USB_MSD_ST* msd, uint8_t* buffer, int len, int* size, uint32_t ms)
{
    int err = 0;

    err = libusb_bulk_transfer(msd->msd_handle, msd->endpoint_in, buffer, len, size, ms);
    if (err < 0)
    {
        return err;
    }

    return 0;
}