/* Input driver header

  Copyright (C) 1995 Free Software Foundation, Inc.

  This file is part of Gforth.

  Gforth is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation; either version 2
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*/

#include <setjmp.h>

extern jmp_buf throw_jmp_buf;

#ifdef MSDOS
#  define prep_terminal()
#  define deprep_terminal()
#  include <conio.h>

#  define key()		getch()
#  define key_query	FLAG(kbhit())
#else
unsigned char getkey(FILE *);
long key_avail(FILE *);
void prep_terminal();
void deprep_terminal();
void get_winsize(void);

#  define key()		getkey(stdin)
#  define key_query	-(!!key_avail(stdin)) /* !! FLAG(...)? - anton */
         		/* flag was originally wrong -- lennart */
#endif

void install_signal_handlers(void);
extern UCell rows, cols;
extern int terminal_prepped;
