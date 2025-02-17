\ MAIN.FS      Kernel main load file                   20may93jaw

\ Authors: Bernd Paysan, Anton Ertl, Jens Wilke, David Kühling, Neal Crook
\ Copyright (C) 1995,1996,1997,1998,2000,2003,2006,2007,2008,2011,2012,2013,2016,2017,2018 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

Create mach-file here over 1+ allot place

0 [IF]
\ debugging: produce a relocation and a symbol table
s" rel-table" r/w create-file throw
Constant fd-relocation-table

\ debuggging: produce a symbol table
s" sym-table" r/w create-file throw
Constant fd-symbol-table
[THEN]


parse-name vocabulary find-name 0= [IF]
    \ if search order stuff is missing assume we are compiling on a gforth
    \ system and include it.
    \ We want the files taken from our current gforth installation
    \ so we don't include relatively to this file
    require ./../startup.fs
[THEN]

\ include etags.fs

include ./../cross.fs              \ cross-compiler

decimal

has? rom 0= [IF]
    has? kernel-start has? kernel-size makekernel
[THEN]
    \ create image-header
has? header [IF]
here 1802 over 
    A,                  \ base address
    0 ,                 \ checksum
    0 ,                 \ image size (without tags)
has? kernel-size
    ,                   \ dict size
    has? stack-size ,   \ data stack size
    has? fstack-size ,  \ FP stack size
    has? rstack-size ,  \ return stack size
    has? lstack-size ,  \ locals stack size
    0 A,                \ boot entry point
    0 A,                \ throw entry point
    0 A,                \ quit entry point
    0 A,                \ execute entry point
    0 A,                \ find entry point
    0 ,                 \ base of DOUBLE_INDIRECT xts[], for comp-i.fs
    0 ,                 \ base of DOUBLE_INDIRECT labels[], for comp-i.fs
[THEN]

doc-off
reset-included
reset-locs

include kernel/aliases.fs             \ primitive aliases, are config-generated
doc-on

has? header [IF]
1802 <> [IF] .s cr .( header start address expected!) cr uffz [THEN]
wheres-off
AConstant image-header
: forthstart image-header @ ;
[THEN]

\ primitive aliases must be before first use, because resolving
\ forward references works only for high level words
[IFUNDEF] r@
' i Alias r@ ( -- w ; R: w -- w ) \ core r-fetch
[THEN]

\ 0 AConstant forthstart

\ include ./vars.fs                  \ variables and other stuff
include kernel/kernel.fs                  \ kernel
\ include ./errore.fs
include kernel/doers.fs
has? file [IF]
    include kernel/args.fs
    include kernel/files.fs               \ file words
    include kernel/paths.fs
    include kernel/require.fs
[THEN]

has? compiler [IF]
    include kernel/cond.fs            \ IF and co.
[THEN]
include kernel/quotes.fs
include kernel/toolsext.fs
include kernel/tools.fs               \ load tools ( .s dump )
include kernel/getdoers.fs
include kernel/copydoers.fs
include kernel/memory.fs

\ Setup                                                13feb93py

include kernel/pass.fs                    \ pass pointers from cross to target

has? header [IF]
    \ set image size
    here image-header 2 cells + !         
    .( set image entry point) cr
    ' boot       >body  image-header #08 cells + !
    ' quit       >body  image-header #10 cells + !
    ' do-execute >body  image-header #11 cells + !
    ' do-find    >body  image-header #12 cells + !
[ELSE]
    >boot
[THEN]

.unresolved                          \ how did we do?
