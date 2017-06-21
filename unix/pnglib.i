%module png
%insert("include")
%{
#include "libpng/png.h"
%}

%apply int { png_size_t, time_t };

#if defined(host_os_linux_android) || defined(host_os_linux_androideabi)
# define __ANDROID__
#endif
#define FAR
#define CHAR_BIT 8

%include "libpng/pngconf.h"
%include "libpng/png.h"
