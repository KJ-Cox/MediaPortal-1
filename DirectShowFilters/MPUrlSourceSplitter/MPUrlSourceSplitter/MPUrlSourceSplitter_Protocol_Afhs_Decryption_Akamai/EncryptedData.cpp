/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#include "StdAfx.h"

#include "EncryptedData.h"

CEncryptedData::CEncryptedData(void)
{
  this->encryptedData = NULL;
  this->encryptedLength = 0;
  this->flvPacket = NULL;
}

CEncryptedData::~CEncryptedData(void)
{
}

/* get methods */

const uint8_t *CEncryptedData::GetEncryptedData(void)
{
  return this->encryptedData;
}


unsigned int CEncryptedData::GetEncryptedLength(void)
{
  return this->encryptedLength;
}

CAkamaiFlvPacket *CEncryptedData::GetAkamaiFlvPacket(void)
{
  return this->flvPacket;
}

/* set methods */

void CEncryptedData::SetEncryptedData(uint8_t *encryptedData)
{
  this->encryptedData = encryptedData;
}

void CEncryptedData::SetEncryptedLength(unsigned int encryptedLength)
{
  this->encryptedLength = encryptedLength;
}

void CEncryptedData::SetAkamaiFlvPacket(CAkamaiFlvPacket *flvPacket)
{
  this->flvPacket = flvPacket;
}

/* other methods */