// this file is in the public domain
%module soil
%insert("include")
%{
#include <SOIL/SOIL.h>
%}

%apply SWIGTYPE * { unsigned char const *const };

%include <SOIL/SOIL.h>
