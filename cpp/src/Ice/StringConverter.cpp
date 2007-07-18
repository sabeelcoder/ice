// **********************************************************************
//
// Copyright (c) 2003-2007 ZeroC, Inc. All rights reserved.
//
// This copy of Ice is licensed to you under the terms described in the
// ICE_LICENSE file included in this distribution.
//
// **********************************************************************

#include <Ice/StringConverter.h>
#include <IceUtil/IceUtil.h>
#include <Ice/LocalException.h>

using namespace IceUtil;
using namespace std;


#ifdef _WIN32
namespace
{
//
// Helper function
//

string getMessageForLastError()
{
    LPVOID lpMsgBuf = 0;
    DWORD ok = FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER |
                             FORMAT_MESSAGE_FROM_SYSTEM |
                             FORMAT_MESSAGE_IGNORE_INSERTS,
                             NULL,
                             GetLastError(),
                             MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
                             (LPTSTR)&lpMsgBuf,
                             0,
                             NULL);

    string msg;
    if(ok)
    {
        msg = (LPCTSTR)lpMsgBuf;
        LocalFree(lpMsgBuf);
    }
    else
    {
        msg = "Unknown Windows error";
    }
    return msg;
}
}
#endif


namespace Ice
{

Byte* 
UnicodeWstringConverter::toUTF8(const wchar_t* sourceStart, 
                                const wchar_t* sourceEnd,
                                UTF8Buffer& buffer) const
{
    //
    // The "chunk size" is the maximum of the number of characters in the
    // source and 6 (== max bytes necessary to encode one Unicode character).
    //
    size_t chunkSize = std::max<size_t>(static_cast<size_t>(sourceEnd - sourceStart), 6);

    Byte* targetStart = buffer.getMoreBytes(chunkSize, 0);
    Byte* targetEnd = targetStart + chunkSize;

    ConversionResult result;

    while((result =
          convertUTFWstringToUTF8(sourceStart, sourceEnd, 
                                  targetStart, targetEnd, lenientConversion))
          == targetExhausted)
    {
        targetStart = buffer.getMoreBytes(chunkSize, targetStart);
        targetEnd = targetStart + chunkSize;
    }
        
    switch(result)
    {
        case conversionOK:
            break;
        case sourceExhausted:
            throw StringConversionException(__FILE__, __LINE__, "wide string source exhausted");
        case sourceIllegal:
            throw StringConversionException(__FILE__, __LINE__, "wide string source illegal");
        default:
        {
            assert(0);
            throw StringConversionException(__FILE__, __LINE__);
        }
    }
    return targetStart;
}


void 
UnicodeWstringConverter::fromUTF8(const Byte* sourceStart, const Byte* sourceEnd,
                                  wstring& target) const
{
    if(sourceStart == sourceEnd)
    {
        target = L"";
        return;
    }

    ConversionResult result = 
        convertUTF8ToUTFWstring(sourceStart, sourceEnd, target, lenientConversion);

    switch(result)
    {    
        case conversionOK:
            break;
        case sourceExhausted:
            throw StringConversionException(__FILE__, __LINE__, "UTF-8 string source exhausted");
        case sourceIllegal:
            throw StringConversionException(__FILE__, __LINE__, "UTF-8 string source illegal");
        default:
        {
            assert(0);
            throw StringConversionException(__FILE__, __LINE__);
        }
    }
}

#ifdef _WIN32
WindowsStringConverter::WindowsStringConverter(unsigned int cp) :
    _cp(cp)
{
}

Byte*
WindowsStringConverter::toUTF8(const char* sourceStart,
                               const char* sourceEnd,
                               UTF8Buffer& buffer) const
{
    //
    // First convert to UTF-16
    //
    int sourceSize = sourceEnd - sourceStart;
    assert(sourceSize > 0);

    size_t size = 0;
    int writtenWchar = 0;
    IceUtil::ScopedArray<wchar_t> wbuffer;
    do
    {
        size = size == 0 ? static_cast<size_t>(sourceSize) + 2 : 2 * size;
        wbuffer.reset(new wchar_t[size]);

        writtenWchar = MultiByteToWideChar(_cp, MB_ERR_INVALID_CHARS, sourceStart,
                                           sourceSize, wbuffer.get(), size);
    } while(writtenWchar == 0 && GetLastError() == ERROR_INSUFFICIENT_BUFFER);

    if(writtenWchar == 0)
    {
        throw StringConversionException(__FILE__, __LINE__, getMessageForLastError());
    }

    //
    // Then convert this UTF-16 wbuffer into UTF-8
    //
    return _unicodeWstringConverter.toUTF8(wbuffer.get(), wbuffer.get() + writtenWchar, buffer);
}

void
WindowsStringConverter::fromUTF8(const Byte* sourceStart, const Byte* sourceEnd,
                                 string& target) const
{
    if(sourceStart == sourceEnd)
    {
        target = "";
        return;
    }

    //
    // First convert to wstring (UTF-16)
    //
    wstring wtarget;
    _unicodeWstringConverter.fromUTF8(sourceStart, sourceEnd, wtarget);

    //
    // And then to a multi-byte narrow string
    //
    size_t size = 0;
    int writtenChar = 0;
    IceUtil::ScopedArray<char> buffer;
    do
    {
        size = size == 0 ? static_cast<size_t>(sourceEnd - sourceStart) + 2 : 2 * size;
        buffer.reset(new char[size]);
        writtenChar = WideCharToMultiByte(_cp, 0, wtarget.data(), wtarget.size(),
                                          buffer.get(), size, 0, 0);
    } while(writtenChar == 0 && GetLastError() == ERROR_INSUFFICIENT_BUFFER);

    if(writtenChar == 0)
    {
        throw StringConversionException(__FILE__, __LINE__, getMessageForLastError());
    }

    target.assign(buffer.get(), writtenChar);
}

#endif

}
