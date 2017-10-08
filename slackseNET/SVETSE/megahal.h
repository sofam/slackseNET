
/*===========================================================================*/

/*
 *  Copyright (C) 1998 Jason Hutchens
 *
 *  This program is free software; you can redistribute it and/or modify it
 *  under the terms of the GNU General Public License as published by the Free
 *  Software Foundation; either version 2 of the license or (at your option)
 *  any later version.
 *
 *  This program is distributed in the hope that it will be useful, but
 *  WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 *  or FITNESS FOR A PARTICULAR PURPOSE.  See the Gnu Public License for more
 *  details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  675 Mass Ave, Cambridge, MA 02139, USA.
 */

/*===========================================================================*/

/*
 *  this file has been modded too
 */
 
#define P_THINK 40
#define D_KEY 100000
#define V_KEY 50000
#define D_THINK 500000
#define V_THINK 250000

#define MIN(a,b) ((a)<(b))?(a):(b)

#define COOKIE "MegaHALv8"

#define DEFAULT "."

#define COMMAND_SIZE (sizeof(command)/sizeof(command[0]))

#define BYTE1 unsigned char
#define BYTE2 unsigned short
#define BYTE4 unsigned long


#define SEP "/"

/*===========================================================================*/

#undef FALSE
#undef TRUE
typedef enum { FALSE, TRUE } bool;

typedef struct {
	BYTE1 length;
	char *word;
} STRING;

typedef struct {
	BYTE4 size;
	STRING *entry;
	BYTE2 *index;
} DICTIONARY;

typedef struct {
	BYTE2 size;
	STRING *from;
	STRING *to;
} SWAP;

typedef struct NODE {
	BYTE2 symbol;
	BYTE4 usage;
	BYTE2 count;
	BYTE2 branch;
	struct NODE **tree;
} TREE;

typedef struct {
	BYTE1 order;
	TREE *forward;
	TREE *backward;
	TREE **context;
	DICTIONARY *dictionary;
} MODEL;

typedef enum { UNKNOWN, QUIT, EXIT, SAVE, DELAY, HELP, LEARN, BRAIN, PROGRESS, THINK } COMMAND_WORDS;

typedef struct {
	STRING word;
	char *helpstring;
	COMMAND_WORDS command;
} COMMAND;

/*===========================================================================*/

#ifdef SUNOS
extern double drand48(void);
extern void srand48(long);
#endif

/*===========================================================================*/

/*
 *		$Log: megahal.h,v $
 * Revision 1.3  1998/09/03  03:15:40  hutch
 * Dunno.
 *
 *		Revision 1.2  1998/04/21 10:10:56  hutch
 *		Fixed a few little errors.
 *
 *		Revision 1.1  1998/04/06 08:02:01  hutch
 *		Initial revision
 */

/*===========================================================================*/

