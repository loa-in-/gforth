\ Compare nonrelocatable images and produce a relocatable image

\ Copyright (C) 1996 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation; either version 2
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program; if not, write to the Free Software
\ Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

s" address-unit-bits" environment? drop constant bits/au
6 constant dodoes-tag

: write-cell { w^ w  file-id -- ior }
    \ write a cell to the file
    w cell file-id write-file ;

: th ( addr1 n -- addr2 )
    cells + ;

: set-bit { u addr -- }
    \ set bit u in bit-vector addr
    u bits/au /mod
    >r 1 bits/au 1- rot - lshift
    r> addr +  cset ;

: compare-images { image1 image2 reloc-bits size file-id -- }
    \G compares image1 and image2 (of size cells) and sets reloc-bits.
    \G offset is the difference for relocated addresses
    \ this definition is certainly to long and too complex, but is
    \ hard to factor.
    image1 @ image2 @ over - { dbase doffset }
    doffset 0= abort" images have the same dictionary base address"
    ." data offset=" doffset . cr
    image1 cell+ @ image2 cell+ @ over - { cbase coffset }
    coffset 0=
    if
	." images have the same code base address; producing only a data-relocatable image" cr
    else
	coffset abs 11 cells <> abort" images produced by different engines"
	." code offset=" coffset . cr
	0 image1 cell+ ! 0 image2 cell+ !
    endif
    size 0
    u+do
	image1 i th @ image2 i th @ { cell1 cell2 }
	cell1 doffset + cell2 =
	if
	    cell1 dbase - file-id write-cell throw
	    i reloc-bits set-bit
	else
	    cell1 coffset + cell2 =
	    if
		cell1 cbase - cell / { tag }
		tag dodoes-tag =
		if
		    \ make sure that the next cell will not be tagged
		    dbase negate image1 i 1+ th +!
		    dbase doffset + negate image2 i 1+ th +!
		endif
		-2 tag - file-id write-cell throw
		i reloc-bits set-bit
	    else
		cell1 file-id write-cell throw
		cell1 cell2 <>
		if
		    0 i th 9 u.r cell1 17 u.r cell2 17 u.r cr
		endif
	    endif
	endif
    loop ;

: slurp-file ( c-addr1 u1 -- c-addr2 u2 )
    \ c-addr1 u1 is the filename, c-addr2 u2 is the file's contents
    r/o bin open-file throw >r
    r@ file-size throw abort" file too large"
    dup allocate throw swap
    2dup r@ read-file throw over <> abort" could not read whole file"
    r> close-file throw ;

: comp-image ( "image-file1" "image-file2" "new-image" -- )
    name slurp-file { image1 size1 }
    image1 size1 s" Gforth1" search 0= abort" not a Gforth image"
    drop 8 + image1 - { header-offset }
    size1 aligned size1 <> abort" unaligned image size"
    size1 image1 header-offset + 2 cells + @ header-offset + <> abort" header gives wrong size"
    name slurp-file { image2 size2 }
    size1 size2 <> abort" image sizes differ"
    name ( "new-image" ) w/o bin create-file throw { outfile }
    size1 header-offset - 1- cell / bits/au / 1+ { reloc-size }
    reloc-size allocate throw { reloc-bits }
    reloc-bits reloc-size erase
    image1 header-offset outfile write-file throw
    base @ hex
    image1 header-offset +  image2 header-offset +  reloc-bits
    size1 header-offset - aligned cell /  outfile  compare-images
    base !
    reloc-bits reloc-size outfile write-file throw
    outfile close-file throw ;

    
