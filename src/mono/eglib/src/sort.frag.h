/* A "counting" merge sort that avoids recursion */

#define N_DIGITS (sizeof (size_t) * 8)

static inline digit
add_digits (digit first, digit second, GCompareFunc func)
{
	/* merge the two lists */
	digit list = NULL;
	digit *pos = &list;
	while (first && second) {
		if (func (first->data, second->data) > 0) {
			*pos = second;
			second = second->next;
		} else {
			*pos = first;
			first = first->next;
		}
		pos = &((*pos)->next);
	}
	*pos = first ? first : second;
	return list;
}

static inline digit
combine_digits (digit *digits, int max_digit, GCompareFunc func)
{
	int i;
	digit list = digits [0];
	for (i = 1; i <= max_digit; ++i)
		list = add_digits (digits [i], list, func);
	return list;
}

static inline int
increment (digit *digits, digit list, int max_digit, GCompareFunc func)
{
	int i;

	if (!list)
		return max_digit;

	for (i = 0; digits [i]; i++) {
		list = add_digits (digits [i], list, func);
		digits [i] = NULL;
		if (i == N_DIGITS-1) /* Should _never_ happen, but if it does, we just devolve into quadratic ;-) */
			break;
	}
	digits [i] = list;
	return i > max_digit ? i : max_digit;
}

static inline digit
do_sort (digit list, GCompareFunc func)
{
	int max_digit = 0;
	digit digits [N_DIGITS]; /* 128 bytes on 32bit, 512 bytes on 64bit */
	memset (digits, 0, sizeof digits);

	while (list && list->next) {
		digit next = list->next;
		digit tail = next->next;

		if (func (list->data, next->data) > 0) {
			next->next = list;
			next = list;
			list = list->next;
		}
		next->next = NULL;

		max_digit = increment (digits, list, max_digit, func);

		list = tail;
	}

	max_digit = increment (digits, list, max_digit, func);

	return combine_digits (digits, max_digit, func);
}

#undef N_DIGITS
