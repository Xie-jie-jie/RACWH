#ifndef _AD5933_h_
#define _AD5933_h_

#include "stm32f1xx_hal.h"
#include <math.h>
#include "i2c.h"

#define AD5933_SYS_Init					0x1000   //(1)<<12  
#define AD5933_Begin_Fre_Scan		0x2000   //(2)<<12
#define AD5933_Fre_UP						0x3000   //(3)<<12
#define AD5933_Fre_Rep					0x4000   //(4)<<12

#define AD5933_Get_Temp					0x9000   //(9)<<12
#define AD5933_Sleep						0xA000   //(10)<<12
#define AD5933_Standby					0xB000   //(11)<<12

#define AD5933_OUTPUT_2V				0x0000   //(0)<<9
#define AD5933_OUTPUT_1V				0x0600   //(3)<<9
#define AD5933_OUTPUT_400mV			0x0400   //(2)<<9
#define AD5933_OUTPUT_200mV			0x0200   //(1)<<9

#define AD5933_Gain_1						0x0100   //(1)<<8
#define AD5933_Gain_5						0x0000   //(0)<<8

#define AD5933_IN_MCLK					0x0000   //(0)<<3
#define AD5933_OUT_MCLK					0x0004   //(1)<<3

#define AD5933_Reset						0x0010   //(1)<<4

#define AD5933 1
#define AD5933_MCLK 16.776  //=536870912/MCLK;

extern long realArr[3],imageArr[3];

//¶¨Òåº¯Êý
void Write_Byte(char nAddr,uint8_t nValue);	//defined
uint8_t Rece_Byte(char nAddr);	//defined
void SetPointer(char nAddr); //defined
uint16_t AD5933_Tempter(void);	//defined
void Scale_imp (uint8_t *SValue,uint8_t *IValue,uint8_t *NValue,uint8_t *CValue);	//defined
int AD5933_Sweep (float Fre_Begin,float Fre_UP,uint16_t UP_Num,uint16_t OUTPUT_Vatage,uint16_t Gain,uint16_t SWeep_Rep);	//defined

#endif
